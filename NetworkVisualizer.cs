using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class NetworkVisualizer : MonoBehaviour
{
    TcpListener listener;  // Server listening for incoming Python data
    TcpClient client;      // Active connection with the Python script
    NetworkStream stream;  // Stream to read incoming data

    public GameObject packetPrefab; // Prefab to represent packets as spheres
    public Transform spawnArea;     // Central area for spawning objects
    public float spawnRadius = 10f; // Radius for random spawning positions

    private List<Vector3> packetPositions = new List<Vector3>();
    public int maxPackets = 50; // Limit the number of spheres and lines

    void Start()
    {
        try
        {
            Debug.Log("Starting TCP server on 127.0.0.1:6000");
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 6000);
            listener.Start();
            Debug.Log("Server started successfully. Waiting for a client to connect...");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start server: {e.Message}");
        }
    }

    void Update()
    {
        // Accept client connection non-blocking
        if (listener.Pending())
        {
            try
            {
                client = listener.AcceptTcpClient();
                stream = client.GetStream();
                Debug.Log($"Client connected from {client.Client.RemoteEndPoint}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error accepting client connection: {e.Message}");
            }
        }

        // Process incoming data from the client
        if (stream != null && stream.DataAvailable)
        {
            try
            {
                byte[] buffer = new byte[1024]; // Buffer to hold incoming data
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log($"Received message: {message}");

                VisualizePacket(); // Render the packet in the scene
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading data from stream: {e.Message}"); // Error message
            }
        }
    }

    void VisualizePacket()
    {
        try
        {
            // Generate random position within the spawn area
            Vector3 randomPosition = spawnArea.position + UnityEngine.Random.insideUnitSphere * spawnRadius;
            randomPosition.y = spawnArea.position.y;

            // Instantiate a sphere to represent the packet
            GameObject packet = Instantiate(packetPrefab, randomPosition, Quaternion.identity);
            float randomScale = UnityEngine.Random.Range(0.1f, 0.3f); // Randomize sphere size
            packet.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

            // Track position for connecting lines
            packetPositions.Add(randomPosition);
            if (packetPositions.Count > maxPackets)
            {
                packetPositions.RemoveAt(0); // Remove the oldest position
            }

            // Draw line to the previous packet (if needed)
            if (packetPositions.Count > 1)
            {
                GameObject lineObj = new GameObject("Line");
                LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

                // Configure LineRenderer
                lineRenderer.startWidth = 0.05f;
                lineRenderer.endWidth = 0.05f;
                lineRenderer.positionCount = 2;
                lineRenderer.useWorldSpace = true;

                lineRenderer.SetPosition(0, packetPositions[packetPositions.Count - 2]);
                lineRenderer.SetPosition(1, packetPositions[packetPositions.Count - 1]);
                Destroy(lineObj, 10f); // Destroy the line after 10 seconds
            }

            Destroy(packet, 10f); // Destroy old sphere after 10 seconds
        }
        catch (Exception e)
        {
            Debug.LogError($"Error visualizing packet: {e.Message}");
        }
    }

    void OnApplicationQuit()
    {
        try
        {
            Debug.Log("Shutting down server...");
            stream?.Close();
            client?.Close();
            listener?.Stop();
            Debug.Log("Server shut down successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during shutdown: {e.Message}");
        }
    }
}
