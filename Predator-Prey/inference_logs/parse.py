import pandas as pd
import sys
RUN_LOG_FILENAME = sys.argv[1]
rows = []
with open(RUN_LOG_FILENAME, "r") as log_file:
    lines = log_file.readlines()

    for line in lines[1:]:
        args = line.split(" ")
        rows.append({
            "episode": int(args[1].replace("]", "")),
            "capture":  "capture" in args[2],
            "lone_wolf_rate": float(args[-1]),
            "prey_survived_step": int(args[11].replace(",", "")),
            "average_predator_distance": float(args[17].replace(",", "")),
            "predator_proximity_rate": float(args[20].replace(",", "")),
        })
df = pd.DataFrame(rows)
df.to_csv(RUN_LOG_FILENAME.replace('.txt', '.csv'), index=False)
