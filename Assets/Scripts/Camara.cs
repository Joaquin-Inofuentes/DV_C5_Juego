using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camara : MonoBehaviour
{
    [SerializeField] Transform target; //El jugador, es el "objetivo" de la cámara
    [SerializeField] float offset;
    [SerializeField] float smoothSpeed;

    private void Update()
    {

    }
    private void LateUpdate()
    {
        Vector3 targetPos = new Vector3(target.position.x, target.position.y, transform.position.z) + target.up * offset;

        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }
}
