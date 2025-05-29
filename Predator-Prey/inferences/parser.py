import pandas as pd
import sys

RUN_LOG_FILENAME = sys.argv[1]
CONVERTERS = {
    "episode": lambda x: int(x),
    "capture": lambda x: bool(x),
    "total_captures": lambda x: int(x),
    "lone_wolf_captures": lambda x: int(x),
    "prey_survived_step": lambda x: int(x),
    "average_predator_distance": lambda x: float(x),
    "predator_proximity_rate": lambda x: float(x),
    "score": lambda x: float(x),
    "capture_x": lambda x: float(x),
    "capture_z": lambda x: float(x),
    "catcher_predator_x": lambda x: float(x),
    "catcher_predator_z": lambda x: float(x),
    "other_predator_x": lambda x: float(x),
    "other_predator_z": lambda x: float(x),
}

rows = []
with open(RUN_LOG_FILENAME, "r") as log_file:
    lines = log_file.readlines()

    for line in lines[1:]:
        data = {}
        args = line.replace("\n", "").strip().split(" ")
        for arg in args:
            key, value = arg.split("=")
            data[key] = CONVERTERS.get(key, lambda x: x)(value)
        rows.append(data)

df = pd.DataFrame(rows)
df.to_csv(RUN_LOG_FILENAME.replace('.txt', '.csv'), index=False)
