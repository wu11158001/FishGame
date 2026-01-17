using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Fusion;

public class WayPoint : MonoBehaviour
{
    [SerializeField] public WayPointEnum WayPointType;
    [SerializeField] public int WayPointId;
    [SerializeField] bool IsSelectShow;
    [SerializeField] Color LineColor = Color.green;

    public List<Transform> Points { get; private set; } = new();

    [ContextMenu(nameof(SetPoint))]
    public void SetPoint()
    {
        Points.Clear();

        Points = transform.GetComponentsInChildren<Transform>().ToList();

        if (Points.Contains(this.transform))
        {
            Points.Remove(this.transform);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!IsSelectShow || Points == null || Points.Count < 2)
            return;

        Gizmos.color = LineColor;

        for (int i = 0; i < Points.Count; i++)
        {
            Vector3 currentPos = Points[i].transform.position;
            Vector3 nextPos;

            if (i < Points.Count - 1)
            {
                nextPos = Points[i + 1].transform.position;
                Gizmos.DrawLine(currentPos, nextPos);
            }

            Gizmos.DrawSphere(currentPos, 0.2f);
        }
    }

    private void OnDrawGizmos()
    {
        if (IsSelectShow || Points == null || Points.Count < 2)
            return;

        Gizmos.color = LineColor;

        for (int i = 0; i < Points.Count; i++)
        {
            Vector3 currentPos = Points[i].transform.position;
            Vector3 nextPos;

            if (i < Points.Count - 1)
            {
                nextPos = Points[i + 1].transform.position;
                Gizmos.DrawLine(currentPos, nextPos);
            }

            Gizmos.DrawSphere(currentPos, 0.2f);
        }
    }
}
