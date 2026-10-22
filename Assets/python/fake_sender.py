import random
import socket
import json
from frame import Frame

class FakeSender:
    def __init__(self, socket: socket.socket):
        self.frame = Frame(landmarks=[], yaw=0, pitch=0, mao_fechada=False)
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.connect(('localhost', 8000)) # definir a conexao e porta

    def send(self, frame):
        for frame in self.get_frame():
            # envia frame no servidor
            self.socket.send(frame.to_json().encode())

    def get_frame(self):
        for _ in range(10):
            self.frame.landmarks = [random.randint(0, 100) for _ in range(33)]
            self.frame.yaw = random.randint(0, 100)
            self.frame.pitch = random.randint(0, 100)
            self.frame.mao_fechada = random.choice([True, False])
            yield self.frame