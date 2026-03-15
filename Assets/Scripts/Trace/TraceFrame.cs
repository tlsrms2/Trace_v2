using UnityEngine;

/// <summary>
/// 트레이스 모드에서 기록할 수 있는 액션 종류
/// </summary>
public enum TraceAction
{
    MOVE,
    ATTACK // (기존 코드 호환을 위해 남겨둠)
}

/// <summary>
/// 트레이스 모드에서 클릭 즉시 기록되는 공격 데이터
/// </summary>
[System.Serializable]
public struct TraceAttackData
{
    public float time; // 기록 시작 후 경과 시간
    public Vector3 position;
    public Vector3 attackDirection;
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
