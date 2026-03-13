using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomSpawnEffect : WaveVisualEffect
{
    [Header("Random Spawn Settings")]
    public GameObject imagePrefab;       // 생성할 이미지 프리팹
    public Transform spawnContainer;     // 이미지들이 담길 부모 (전체 화면 크기의 패널)
    public int maxImagesToFill = 50;     // 웨이브 완료 시 꽉 차게 될 목표 이미지 개수

    private List<GameObject> spawnedImages = new List<GameObject>();
    private RectTransform containerRect;

    void Awake()
    {
        containerRect = spawnContainer.GetComponent<RectTransform>();
    }

    protected override void ApplyVisuals(float progress)
    {
        // 1. 현재 진행도에 비례하여 '있어야 할 이미지의 개수' 계산
        // (요동치는 progress 값을 사용하므로 개수가 줄었다 늘었다 할 수 있음)
        int targetImageCount = Mathf.RoundToInt(maxImagesToFill * progress);

        // 2. 이미지가 부족하면 생성
        while (spawnedImages.Count < targetImageCount)
        {
            SpawnImage();
        }

        // 3. 이미지가 너무 많으면 삭제 (요동치는 연출 때문에 깜빡이듯 켜지고 꺼질 수 있음)
        while (spawnedImages.Count > targetImageCount && spawnedImages.Count > 0)
        {
            int lastIndex = spawnedImages.Count - 1;
            Destroy(spawnedImages[lastIndex]);
            spawnedImages.RemoveAt(lastIndex);
        }

        // 4. 추가 연출: 스폰된 이미지들의 크기나 투명도도 같이 요동치게 만들 수 있습니다.
        // float alphaScale = 0.5f + (progress * 0.5f); 
        // 등 원하는 추가 애니메이션 적용 가능
    }

    public override void ResetEffect()
    {
        base.ResetEffect();
        
        // 생성된 모든 이미지 파괴
        foreach (var img in spawnedImages)
        {
            if (img != null) Destroy(img);
        }
        spawnedImages.Clear();
    }

    private void SpawnImage()
    {
        if (imagePrefab == null || containerRect == null) return;

        GameObject newImg = Instantiate(imagePrefab, spawnContainer);
        RectTransform rect = newImg.GetComponent<RectTransform>();

        // 컨테이너 범위 내에서 랜덤 위치 계산
        float randomX = Random.Range(containerRect.rect.xMin, containerRect.rect.xMax);
        float randomY = Random.Range(containerRect.rect.yMin, containerRect.rect.yMax);

        rect.localPosition = new Vector2(randomX, randomY);
        spawnedImages.Add(newImg);
    }

    // 웨이브가 넘어갈 때 기존 이미지 초기화가 필요하다면 호출
    public void ClearImages()
    {
        foreach (var img in spawnedImages) Destroy(img);
        spawnedImages.Clear();
    }
}