using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PrototypeGenerator
{
    public static void Generate(string description)
    {
        if (description.ToLower().Contains("salto"))
        {
            //Prueba para ver como se generan 
            GenerateJumpScript();
        }
        else
        {
            Debug.Log("Mecánica no reconocida");
        }
    }

    private static void GenerateJumpScript()
    {
        string script = @"
using UnityEngine;

public class JumpController : MonoBehaviour
{
    public float jumpForce = 5f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}";

        string path = "Assets/TFGToolkit/Scripts/Generated/JumpController.cs";
        File.WriteAllText(path, script);

        Debug.Log("Script generado.");
        AssetDatabase.Refresh();
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            player.AddComponent(componentType: System.Type.GetType("JumpController"));
            Debug.Log("Añadido al jugador");
        }
        else
        {
            Debug.Log("No se ha encontrado al jugador");
        }
    }
}
