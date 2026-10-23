import cv2
import urllib.request
from pathlib import Path
import mediapipe as mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
import numpy as np
import math


def _find_project_root() -> Path:
    module_directory = Path(__file__).resolve().parent
    for directory in (module_directory, *module_directory.parents):
        if (directory / ".git").exists():
            return directory
    return module_directory


class HandDetection:
    """Encapsula detecção de mãos com MediaPipe Tasks API."""

    # Pares de conexões entre os 21 pontos (estrutura padrão da mão)
    CONNECTIONS = [
        (0, 1), (1, 2), (2, 3), (3, 4),          # Polegar
        (0, 5), (5, 6), (6, 7), (7, 8),          # Indicador
        (5, 9), (9, 10), (10, 11), (11, 12),     # Médio
        (9, 13), (13, 14), (14, 15), (15, 16),   # Anelar
        (13, 17), (17, 18), (18, 19), (19, 20),  # Mínimo
        (0, 17)                                   # Palma
    ]

    MODEL_URL = "https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task"
    MODEL_NAME = "hand_landmarker.task"

    def __init__(self, model_path: str | Path | None = None,
                 maxHands=2):

        self.maxHands = maxHands

        project_model_path = _find_project_root() / self.MODEL_NAME
        self._downloadModel(project_model_path)
        model_asset_path = Path(model_path) if model_path is not None else project_model_path

        base_options = python.BaseOptions(model_asset_path=str(model_asset_path))

        options = vision.HandLandmarkerOptions(
                    base_options=base_options,
                    num_hands=self.maxHands,
                    running_mode=vision.RunningMode.VIDEO
                )
        
        self.detector = vision.HandLandmarker.create_from_options(options)

        self.timestamp = 0

    def _downloadModel(self, model_path: Path):
        if not model_path.exists():
            print("Downloading MediaPipe Hand model...")
            urllib.request.urlretrieve(self.MODEL_URL, str(model_path))
            print("Download complete.")

    def process_frame(self, frame):

        # Converte BGR -> RGB e cria objeto mp.Image
        rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)

        # Timestamp em ms (obrigatório no modo VIDEO)
        result = self.detector.detect_for_video(mp_image, self.timestamp)
        self.timestamp += 1

        if not result.hand_landmarks:
            return None

        h, w, _ = frame.shape

        # Usamos apenas a primeira mão (ajuste se quiser várias)
        hand = result.hand_landmarks[0]
        myHand = []

        # Desenha conexões
        for start, end in self.CONNECTIONS:
            p1, p2 = hand[start], hand[end]
            cv2.line(frame,
                 (int(p1.x * w), int(p1.y * h)),
                 (int(p2.x * w), int(p2.y * h)),
                 (0, 255, 0), 2)

        # Desenha pontos e monta lista
        yaw, pitch = self.yaw_pitch_detection(hand, w, h)
        for lm in hand:
            # desenho: converte para pixels porque cv2.circle trabalha em pixels.
            # myHand: mantém normalizado (0..1) porque o Unity espera essa escala.
            cx, cy = int(lm.x * w), int(lm.y * h)
            cv2.circle(frame, (cx, cy), 4, (0, 0, 255), -1)
            myHand.append([lm.x, lm.y, lm.z])

        mao_fechada = self.mao_fechada_detection(hand, w, h)
        return myHand, yaw, pitch, mao_fechada

    def yaw_pitch_detection(self, landmarks, w, h):

        def _3d_coordinates(hand_index):
            return np.array([landmarks[hand_index].x * w, -landmarks[hand_index].y * h, -landmarks[hand_index].z * w])

        d = _3d_coordinates(9) - _3d_coordinates(0)
        d /= np.linalg.norm(d)

        yaw   = math.degrees(math.atan2(d[0], d[2]))                     # esquerda/direita
        pitch = math.degrees(math.atan2(d[1], math.hypot(d[0], d[2])))  # cima/baixo
        return yaw, pitch


    def mao_fechada_detection(self, hand, w, h):
        def dist(i, j):
            dx = (hand[i].x - hand[j].x) * w
            dy = (hand[i].y - hand[j].y) * h
            return math.hypot(dx, dy)

        palma = dist(0, 9)
        if palma == 0:
            return False

        # conta dedos estendidos: ponta longe da base do dedo
        estendidos = sum(
            1 for ponta, base in [(8, 5), (12, 9), (16, 13), (20, 17)]
            if dist(ponta, base) / palma > 0.6
        )

        return estendidos <= 1   # fechada = no máximo 1 dedo estendido

    def close(self):
        self.detector.close()