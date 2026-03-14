using UnityEngine;

/// <summary>
/// 트레이스 모드에서 기록할 수 있는 액션 종류
/// </summary>
public enum TraceAction
{
    MOVE,
    ATTACK
}

/// <summary>
/// 트레이스 모드에서 한 프레임의 기록 데이터.
/// </summary>
[System.Serializable]
public struct TraceFrame
{
    public Vector3 position;
    public Quaternion rotation;
    public TraceAction action;
    public Vector3 attackDirection;

    public TraceFrame(Vector3 position, Quaternion rotation, TraceAction action, Vector3 attackDir = default)
    {
        this.position = position;
        this.rotation = rotation;
        this.action = action;
        this.attackDirection = attackDir;
    }
}
