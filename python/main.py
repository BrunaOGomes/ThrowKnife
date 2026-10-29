import cv2
import socket
import time

from hand_detection import HandDetection
from frame import Frame

# Parâmetros da câmera
width, height = 1280, 720

cap = cv2.VideoCapture(0)
cap.set(3, width)
cap.set(4, height)

detector = HandDetection(maxHands=1)

# Comunicação UDP
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
serverAddressPort = ('127.0.0.1', 6767)

try:
    while True:
        success, img = cap.read()
        img = cv2.flip(img, 1)  # Espelha a imagem horizontalmente
        if not success:
            print("Câmera falhou, reconectando...")
            cap.release()
            time.sleep(0.5)
            cap = cv2.VideoCapture(0)
            cap.set(3, width)
            cap.set(4, height)
            continue

        resultado = detector.process_frame(img)

        if resultado is not None:
            landmarks, yaw, pitch, mao_fechada = resultado
            frame = Frame(
                landmarks=landmarks,
                yaw=yaw,
                pitch=pitch,
                mao_fechada=mao_fechada,
            )
            try:
                sock.sendto(frame.to_json().encode(), serverAddressPort)
            except OSError as erro:
                print(f"UDP: {erro}")


        img = cv2.resize(img, (0, 0), None, 0.5, 0.5)
        cv2.imshow("Webcam", img)
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break
finally:
    detector.close()
    cap.release()
    cv2.destroyAllWindows()
