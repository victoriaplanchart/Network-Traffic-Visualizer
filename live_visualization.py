import socket
import subprocess
import time

# Define constants for the Unity connection and tcpdump command
UNITY_HOST = "127.0.0.1"  # Unity server IP
UNITY_PORT = 6000          # Port to send data to Unity
INTERFACE = "en0"          # Network interface

# Start tcpdump as a subprocess to capture live network packets
try:
    process = subprocess.Popen(
        ["sudo", "tcpdump", "-i", INTERFACE, "-n", "-s", "0"],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE
    )
except Exception as e:
    print(f"Error starting tcpdump: {e}")
    exit(1)

# Establish a socket connection to Unity
client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
for attempt in range(5):  # Retry loop for socket connection
    try:
        client.connect((UNITY_HOST, UNITY_PORT))
        print("Connected to Unity.")
        break
    except ConnectionRefusedError:
        print("Connection refused. Retrying...")
        time.sleep(2)
else:
    print("Failed to connect after 5 attempts.")
    process.terminate()
    exit(1)

# Process packets from tcpdump and send them to Unity
try:
    for line in iter(process.stdout.readline, b""):  # Read tcpdump output line by line
        line = line.decode("utf-8").strip()
        parts = line.split()

        # Check if the line contains valid packet information
        if len(parts) < 5 or ':' not in parts[4]:
            continue  # Skip malformed lines

        # Extract source and destination IP addresses
        src, dest = parts[2], parts[4].strip(":")
        packet_data = f"Source: {src}, Destination: {dest}\n"

        # Send the formatted packet data to Unity
        client.send(packet_data.encode("utf-8"))

except KeyboardInterrupt:
    print("Script interrupted by user.")

finally:
    print("Shutting down tcpdump and socket...")
    process.terminate()
    client.close()
    print("Cleanup completed successfully.")
