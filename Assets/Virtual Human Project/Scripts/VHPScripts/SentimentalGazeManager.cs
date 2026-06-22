using System.Collections.Generic;
using UnityEngine;

public class SentimentalGazeManager : MonoBehaviour
{
    [Header("Sentimental Attention Settings")]
    [Tooltip("Lista de objetos que o NPC deve dar preferência emocional no olhar.")]
    public List<GameObject> sentimentalObjects = new List<GameObject>();

    [Tooltip("Multiplicador do peso para aumentar a prioridade dos objetos sentimentais.")]
    public float sentimentalMultiplier = 5.0f;

    private VHPGazeTarget vhpGazeTarget;
    private bool isHooked = false;

    private void Update()
    {
        // O VHP cria o Eyes_Target dinamicamente, então buscamos a referência no Update até achá-la.
        if (!isHooked)
        {
            vhpGazeTarget = GetComponentInChildren<VHPGazeTarget>();
            
            if (vhpGazeTarget != null)
            {
                // Assim que o script do VHP for instanciado, nos injetamos a escuta do evento de peso
                vhpGazeTarget.OnCalculateTargetWeight += AlterarPesoSentimental;
                isHooked = true;
            }
        }
    }

    private void OnDisable()
    {
        if (isHooked && vhpGazeTarget != null)
        {
            vhpGazeTarget.OnCalculateTargetWeight -= AlterarPesoSentimental;
            isHooked = false;
        }
    }

    /// <summary>
    /// Este método é chamado automaticamente pelo VHPGazeTarget toda vez que ele está calculando o peso de um alvo do InterestField.
    /// </summary>
    private float AlterarPesoSentimental(Transform target, float currentWeight)
    {
        // O collider pode estar no pai ou num filho do modelo, então verificamos a hierarquia
        if (IsSentimentalObject(target.gameObject))
        {
            // O peso original pode ser 0 ou muito baixo se o objeto estiver longe. 
            // Adicionamos um valor base alto (ex: 50) e depois multiplicamos para GARANTIR a atenção máxima.
            float novoPeso = (currentWeight + 50f) * sentimentalMultiplier;
            
            // Log para você visualizar no console se o sistema está detectando e alterando o peso
            Debug.Log($"[Sentimental Gaze] Alvo Emocional '{target.name}' processado! Peso subiu de {currentWeight} para {novoPeso}");
            
            return novoPeso;
        }

        return currentWeight;
    }

    private bool IsSentimentalObject(GameObject obj)
    {
        // Verifica se o objeto atual ou algum parent dele está na lista
        Transform current = obj.transform;
        while (current != null)
        {
            if (sentimentalObjects.Contains(current.gameObject))
                return true;
            current = current.parent;
        }
        return false;
    }
}
