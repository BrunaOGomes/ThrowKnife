



import cv2
from hand_detection import HandDetection
from frame import Frame
import socket

# Parâmetros da câmera
width, height = 1280, 720

cap = cv2.VideoCapture(1)
cap.set(3, width)  # Set width
cap.set(4, height)   # Set height

detector = HandDetection(maxHands=1)

# Comunicação UDP
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
serverAddressPort = ('127.0.0.1', 6767)

while True:
    success, img = cap.read()
    if not success:
        break

    resultado = detector.process_frame(img)

    w, h, _ = img.shape

    # Landmark valores - (x, y, z) * 21
    if resultado is not None:
        landmarks, yaw, pitch, mao_fechada = resultado
        frame = Frame(landmarks=landmarks, yaw=yaw, pitch=pitch, mao_fechada=mao_fechada)
        sock.sendto(frame.to_json().encode(), serverAddressPort)
    
    
    img = cv2.resize(img, (0, 0), None, 0.5, 0.5)  # Reduzir a imagem para metade do tamanho
    cv2.imshow("Webcam", img)
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break