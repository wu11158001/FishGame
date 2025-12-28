using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour
{
    // 同步角度變數
    [Networked] public float NetworkedAngle { get; set; }

    bool IsSetLocalPosition;

    public override void Spawned()
    {
        
    }

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

    public override void Render()
    {
        if(!IsSetLocalPosition && transform.parent != null)
        {
            IsSetLocalPosition = true;

            transform.localPosition = Vector3.zero;
            Debug.Log("我在裡面");
        }
        else
        {
            if(!IsSetLocalPosition)
            Debug.Log("我在外面");
        }
    }
}
