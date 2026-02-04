import sys
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SCRIPT = ROOT / "python_analysis" / "vr_analysis.py"

print(f"[INFO] Python usado: {sys.executable}")
print(f"[INFO] Ejecutando análisis: {SCRIPT}")

subprocess.run([sys.executable, str(SCRIPT)], check=True)

print("[INFO] Análisis finalizado.")
