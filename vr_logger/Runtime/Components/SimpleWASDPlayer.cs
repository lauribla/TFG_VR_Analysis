using UnityEngine;

/// <summary>
/// Script de movimiento ultra-sencillo (WASD + Ratón) para probar un entorno
/// sin tener puestas las gafas de Realidad Virtual.
/// </summary>
public class SimpleWASDPlayer : MonoBehaviour
{
    [Header("Configuración de Velocidad")]
    public float speed = 5.0f;
    public float mouseSensitivity = 2.0f;

    [Header("Referencias")]
    [Tooltip("Arrastra aquí la cámara de tu jugador (Suele ser Camera.main). Si está pre-asignada, mejor.")]
    public Camera playerCamera;

    private float verticalRotation = 0f;

    private void Start()
    {
        // 1. Esconder y bloquear el ratón
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 2. Intentar buscar la cámara si se nos olvidó arrastrarla
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        // Movimiento WASD
        float horizontal = Input.GetAxisRaw("Horizontal"); // A, D, Flechas Izq/Der
        float vertical = Input.GetAxisRaw("Vertical");     // W, S, Flechas Arr/Aba

        // Calcular dirección con normalización (evita moverse un 40% más rápido en diagonal)
        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

        transform.Translate(direction * speed * Time.deltaTime, Space.Self);

        // Rotación de Cabeza y Cuerpo (Ratón)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Girar el cuerpo entero hacia los lados
        transform.Rotate(Vector3.up * mouseX);

        // Girar solo la cámara arriba y abajo
        if (playerCamera != null)
        {
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -85f, 85f); // Evita romperte el cuello hacia atrás
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        // Sistema de rescate: Pulsa Escape para liberar el ratón y cerrar el juego
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
