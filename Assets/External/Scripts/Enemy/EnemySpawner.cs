using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private float xOffset = 15.0f;
    [SerializeField] private float yOffset = 10.0f;
    private Transform _playerTransform;

    void Awake()
    {
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void SpawnEnemy(GameObject enemyPrefab)
    {
        int flag = Random.Range(0, 2);
        Vector3 playerPosition = _playerTransform.position;
        Vector3 randomPosition = Vector3.zero;

        float x = 0.0f;
        float y = 0.0f;
        if (flag == 0)
        {
            x = playerPosition.x;
            x += Random.Range(0, 2) == 0 ? -xOffset : xOffset;
            y = Random.Range(playerPosition.y - yOffset, playerPosition.y + yOffset);
        }
        else
        {
            x = Random.Range(playerPosition.x - xOffset, playerPosition.x + xOffset);
            y = playerPosition.y;
            y += Random.Range(0, 2) == 0 ? -yOffset : yOffset;
        }
        randomPosition = new Vector3(x, y, 0);
        Instantiate(enemyPrefab, randomPosition, Quaternion.identity);
    }

    // private void OnDrawGizmos()
    // {
    //     if (_playerTransform == null)
    //     {
    //         GameObject player = GameObject.FindGameObjectWithTag("Player");
    //         if (player != null) _playerTransform = player.transform;
    //         else return;
    //     }

    //     float xOffset = this.xOffset;
    //     float yOffset = this.yOffset;
    //     Vector3 center = _playerTransform.position;

    //     Gizmos.color = Color.red;

    //     Vector3 size = new Vector3(xOffset * 2, yOffset * 2, 0);
    //     Gizmos.DrawWireCube(center, size);

    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawSphere(new Vector3(center.x + xOffset, center.y, 0), 0.5f); 
    //     Gizmos.DrawSphere(new Vector3(center.x - xOffset, center.y, 0), 0.5f); 
    //     Gizmos.DrawSphere(new Vector3(center.x, center.y + yOffset, 0), 0.5f); 
    //     Gizmos.DrawSphere(new Vector3(center.x, center.y - yOffset, 0), 0.5f); 
    // }
}
