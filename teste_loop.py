import time

print("AURA: processo iniciado", flush=True)

for i in range(10):
    print(f"AURA: execução {i + 1}/10", flush=True)
    time.sleep(5)

print("AURA: processo finalizado", flush=True)
