using CustomInspector;
using System.Collections.Generic;
using UnityEngine;

public class IA_P2_PathfindingModel : MonoBehaviour
{
    [Button(nameof(OnEnable))]
    [Header("Configuración de Vecinos")]
    public float sideOffset = 0.5f; // ancho del agente

    [Header("Capa de obstáculos")]
    public LayerMask obstacleLayer;

    [Header("Puntos del Grafo")]
    public List<IA_P2_PathNode> allNodes = new List<IA_P2_PathNode>();

    public static IA_P2_PathfindingModel Instance;


    public void Awake()
    {
        Instance = this;
    }

    public void OnEnable()
    {
        Instance = this;
        ReCalcularVecinos();
    }



    // ---------------------------------------------------------
    //   BUSCAR AUTOMÁTICAMENTE TODOS LOS NODOS DEL ESCENA
    // ---------------------------------------------------------
    public void ReCalcularVecinos()
    {
        allNodes = new List<IA_P2_PathNode>(FindObjectsOfType<IA_P2_PathNode>());
        GenerateNeighbors();
    }

    // ---------------------------------------------------------
    //   GENERA TODOS LOS VECINOS
    // ---------------------------------------------------------
    public void GenerateNeighbors()
    {
        foreach (var node in allNodes)
        {
            GenerateNeighborsForNode(node);
        }
    }

    public void GenerateNeighborsForNode(IA_P2_PathNode node)
    {
        if (node == null) return;
        node.Vecinos.Clear();

        foreach (var other in allNodes)
        {
            if (other == null || other == node) continue;

            Vector3 start = node.transform.position;
            Vector3 end = other.transform.position;
            Vector3 dir = (end - start).normalized;

            // CAMBIO CLAVE: En YX, el vector perpendicular se obtiene 
            // cruzando la dirección con el eje Z (Vector3.forward)
            Vector3 perpendicular = Vector3.Cross(dir, Vector3.forward).normalized;

            bool centerClear = IA_P2_LineOfSight3D.Check(start, end, obstacleLayer);
            if (!centerClear) continue;

            // Laterales (para ver si el agente cabe por el hueco en 2D)
            Vector3 startSideA = start - perpendicular * sideOffset;
            Vector3 startSideB = start + perpendicular * sideOffset;
            Vector3 endSideA = end - perpendicular * sideOffset;
            Vector3 endSideB = end + perpendicular * sideOffset;

            if (IA_P2_LineOfSight3D.Check(startSideA, endSideA, obstacleLayer) &&
                IA_P2_LineOfSight3D.Check(startSideB, endSideB, obstacleLayer))
            {
                node.Vecinos.Add(other);
            }
        }
    }





}
