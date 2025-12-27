using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour
{
    // 同步角度變數
    [Networked] public float NetworkedAngle { get; set; }

    public override void FixedUpdateNetwork()   
    {
        // 轉向
        if (GetInput(out NetworkInputData input))
        {
            float distanceToCamera = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(input.MousePosition.x, input.MousePosition.y, distanceToCamera));
            Vector2 dir = (Vector2)mouseWorldPos - (Vector2)transform.position;

            NetworkedAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        transform.rotation = Quaternion.Euler(0, 0, NetworkedAngle);
    }
}
