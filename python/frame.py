import json
import time

class Frame:
    def __init__(self, landmarks, yaw, pitch, mao_fechada):
        self.landmarks = landmarks
        self.yaw = yaw
        self.pitch = pitch
        self.mao_fechada = mao_fechada
        self.timestamp = time.time()

    def to_dict(self):
        return {
            "tipo": "frame",
            "timestamp": self.timestamp,
            "mao_fechada": self.mao_fechada,
            "yaw": self.yaw,
            "pitch": self.pitch,
            "landmarks": self.landmarks,
        }

    def to_json(self):
        return json.dumps(self.to_dict())