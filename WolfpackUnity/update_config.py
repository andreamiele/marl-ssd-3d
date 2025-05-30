import yaml
import argparse
import os

def update_config(base_config_path, updates, output_config_path):
    # Load base YAML config
    with open(base_config_path, 'r') as file:
        config = yaml.safe_load(file)

    # Apply updates (supports nested keys using dot notation)
    for key, value in updates.items():
        keys = key.split(".")
        d = config
        for k in keys[:-1]:
            d = d.setdefault(k, {})  # Create sub-dict if needed
        d[keys[-1]] = value

    # Write the updated config to a new file
    with open(output_config_path, 'w') as file:
        yaml.safe_dump(config, file)

    print(f"Updated config saved to: {output_config_path}")

def parse_args():
    parser = argparse.ArgumentParser(description="Update ML-Agents YAML config.")
    parser.add_argument("base_config", help="Path to base YAML config file")
    parser.add_argument("output_config", help="Path to save the updated config file")
    parser.add_argument("--param", action='append', help="Parameter to update in the form key=value. Use dot notation for nested keys.")

    return parser.parse_args()

def parse_updates(param_list):
    updates = {}
    for item in param_list:
        if '=' not in item:
            raise ValueError(f"Invalid format for parameter: {item}. Expected key=value.")
        key, value = item.split('=', 1)
        # Try to cast value to int or float if possible
        if value.isdigit():
            value = int(value)
        else:
            try:
                value = float(value)
            except ValueError:
                pass  # keep as string
        updates[key] = value
    return updates

if __name__ == "__main__":
    args = parse_args()
    updates = parse_updates(args.param)
    update_config(args.base_config, updates, args.output_config)