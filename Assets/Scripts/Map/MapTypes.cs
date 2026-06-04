using UnityEngine;

/// <summary>
/// 지도 시스템 공용 타입.
/// 방은 정수 그리드 칸(열,행)에 배치되며, 연결은 칸 이동(step)으로 표현된다.
/// </summary>
public enum MapDir { Right, Left, Up, Down }

/// <summary>
/// ZoneTransition에서 지도 방향을 수동 지정할 때 사용.
/// Auto면 문 위치로 자동 판별(상하좌우). 대각은 2층 분기 등 수동 전용.
/// </summary>
public enum MapDirOverride { Auto, Right, Left, Up, Down, UpLeft, UpRight, DownLeft, DownRight }

/// <summary>한 방에서 이웃 방으로의 연결.</summary>
public struct MapConnection
{
    public string     target; // 이웃 방(씬) 이름. 아직 미방문일 수 있음.
    public MapDir      dir;    // 카디널 방향(실제 문 위치 기반). stub 그리기용.
    public Vector2Int  step;   // 그리드 칸 이동(override 반영). 배치용.

    public MapConnection(string target, MapDir dir, Vector2Int step)
    {
        this.target = target;
        this.dir    = dir;
        this.step   = step;
    }
}

/// <summary>방향 유틸리티.</summary>
public static class MapDirUtil
{
    /// <summary>(x,y) 단위 벡터. Right=(1,0), Left=(-1,0), Up=(0,1), Down=(0,-1).</summary>
    public static Vector2 ToVector(MapDir d) => d switch
    {
        MapDir.Right => Vector2.right,
        MapDir.Left  => Vector2.left,
        MapDir.Up    => Vector2.up,
        _            => Vector2.down,
    };

    /// <summary>정규화 변위에서 지배적인 축의 카디널 방향을 고른다.</summary>
    public static MapDir FromDelta(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return delta.x >= 0f ? MapDir.Right : MapDir.Left;
        return delta.y >= 0f ? MapDir.Up : MapDir.Down;
    }

    /// <summary>카디널 방향 → 그리드 칸 이동.</summary>
    public static Vector2Int StepFromDir(MapDir d) => d switch
    {
        MapDir.Right => new Vector2Int(1, 0),
        MapDir.Left  => new Vector2Int(-1, 0),
        MapDir.Up    => new Vector2Int(0, 1),
        _            => new Vector2Int(0, -1),
    };

    /// <summary>override 값 → 그리드 칸 이동(대각 포함). Auto는 호출 전에 거른다.</summary>
    public static Vector2Int StepFromOverride(MapDirOverride o) => o switch
    {
        MapDirOverride.Right     => new Vector2Int(1, 0),
        MapDirOverride.Left      => new Vector2Int(-1, 0),
        MapDirOverride.Up        => new Vector2Int(0, 1),
        MapDirOverride.Down      => new Vector2Int(0, -1),
        MapDirOverride.UpLeft    => new Vector2Int(-1, 1),
        MapDirOverride.UpRight   => new Vector2Int(1, 1),
        MapDirOverride.DownLeft  => new Vector2Int(-1, -1),
        MapDirOverride.DownRight => new Vector2Int(1, -1),
        _                        => Vector2Int.zero,
    };
}
