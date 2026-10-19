class Frame:
    def __init__(self, landmarks, yaw, pitch, mao_fechada):
        self.landmarks = landmarks
        self.yaw = yaw
        self.pitch = pitch
        self.mao_fechada = mao_fechada

    def to_dict(self):
        return {
            "landmarks": self.landmarks,
            "yaw": self.yaw,
            "pitch": self.pitch,
            "mao_fechada": self.mao_fechada
        }