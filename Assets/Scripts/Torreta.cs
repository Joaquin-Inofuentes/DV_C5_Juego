using USP.Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torreta : MonoBehaviour
{
    public GameObject TorretaPrefab;
    public Transform TorretaTransform;
    public float Rango = 5;
    public Transform player;
    // Start is called before the first frame update
    void Start()
    {
        TorretaTransform = transform;
        TorretaPrefab = gameObject;
        if (GameManager.player != null)
            player = GameManager.player.transform;
        else
            Debug.LogWarning("[Torreta] GameManager.player es null; la torreta no apuntará hasta que exista el jugador.");
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;
        //Debug.Log(Vector2.Distance(transform.position, player.position));
        if (Vector2.Distance(transform.position, player.position) < Rango)
        {
            transform.up = (player.position-transform.position).normalized;
        }
    }
}

