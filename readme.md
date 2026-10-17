# Descição do ThrowKnife

## 🎯 Simulador de Lançamento de Faca em Balões

Simulador 3D onde o jogador lança facas contra balões usando o movimento real da mão, capturado por uma **webcam frontal**. A mão do jogador é rastreada em tempo real e renderizada na cena, e o arremesso é detectado a partir do gesto natural de **fechar e abrir a mão**. Após o lançamento, a faca segue uma trajetória balística influenciada pela gravidade.

Projeto desenvolvido como **trabalho de faculdade**, com foco em demonstrar a integração entre **visão computacional (MediaPipe Hands)** e **um motor de jogo 3D (Unity)** para gerar uma experiência interativa simples e intuitiva.

---

## 📌 Visão Geral

O jogador posiciona-se em frente a uma webcam. O sistema detecta os **21 pontos da mão** via **MediaPipe Hands**, extrai a **posição**, os **ângulos (yaw e pitch)** e o **estado da mão (aberta/fechada)**. Esses dados são enviados em tempo real para o Unity, que:

- **Renderiza a mão em 3D** na cena.
- Detecta o gesto de **pegar uma faca** do estoque.
- Detecta o gesto de **lançamento**.
- Aplica **física 3D** na faca (gravidade + movimento livre em XZ).
- Verifica **colisão com os balões** e atualiza a pontuação.

O objetivo é acertar os 3 balões com as 3 facas disponíveis. Cada faca lançada ou acerta um balão ou é perdida ao sair da cena. Ao final das 3 facas, o jogo exibe a pontuação e reinicia.

---

## 🎬 Objetivo do Projeto

Produzir um **vídeo demonstrativo** de um simulador funcional onde o jogador lança facas em balões usando a mão como controle. O foco é a **prova de conceito** da integração entre captura de movimento, renderização 3D e física em tempo real, não um jogo completo.

# Diagrama de Funcionamento

![ThrowKnife](image.png)

---

# INTEGRANTES

Bruna Oliveira Gomes - 553135  
Enzo Gabriel Nicolosi Croquer - 553213  
Felipe Lima Bonato Testa - 553780  
Rodrigo Silva Rocha - 552857  
Samuel Guilherme Monte - 554154  
Thiago Luiz Pereira - 553720
