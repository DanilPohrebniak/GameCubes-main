using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DiceManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject cubePrefab;
    public int cubeCount = 6;
    public float throwForce = 2f;
    public float spawnOffset = 0.5f;
    public float tableHalfWidth = 1.4f;

    private List<Dice> dices = new List<Dice>();
    private List<GameObject> spawnedCubes = new List<GameObject>();
    public IReadOnlyList<Dice> Dices => dices;

    public void SpawnAndThrow(Transform spawnOrigin)
    {
        if (spawnedCubes.Count == 0)
        {
            for (int i = 0; i < cubeCount; i++)
            {
                GameObject cube = Instantiate(cubePrefab, Vector3.zero, Quaternion.identity);
                cube.transform.localScale = Vector3.one * 0.33f;
                spawnedCubes.Add(cube);

                Dice dice = cube.GetComponent<Dice>();
                if (dice != null) dices.Add(dice);
            }
        }

        Vector3 forward = spawnOrigin.forward;
        Vector3 right = spawnOrigin.right;

        foreach (var cube in spawnedCubes)
        {
            DiceController controller = cube.GetComponent<DiceController>();

            // Если кубик отложен - пропускаем его
            if (controller != null && controller.IsLocked) continue;

            // Обязательно включаем кубик перед броском (на случай "Горячих костей")
            cube.SetActive(true);

            float sideOffset = Random.Range(-tableHalfWidth, tableHalfWidth);
            cube.transform.position = spawnOrigin.position + right * sideOffset + forward * spawnOffset;

            Rigidbody rb = cube.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                Vector3 throwDir = forward + new Vector3(0f, Random.Range(-0.05f, 0.05f), 0f);
                rb.AddForce(throwDir.normalized * throwForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 1f, ForceMode.Impulse);
            }

            if (controller != null) controller.CurrentValue = 0;
        }
    }

    // ИСПРАВЛЕНИЕ: Ждем остановки ТОЛЬКО тех кубиков, которые сейчас активны (видимы) на сцене
    public bool AllStopped() => dices.Where(d => d.gameObject.activeInHierarchy).All(d => d.IsStopped());

    public void FullReset()
    {
        foreach (GameObject cube in spawnedCubes)
        {
            DiceController controller = cube.GetComponent<DiceController>();
            if (controller != null) controller.ResetDice();
        }
    }
}