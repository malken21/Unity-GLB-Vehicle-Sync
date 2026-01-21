using UnityEngine;

[System.Serializable]
public class PIDController
{
    public float pFactor, iFactor, dFactor; 

    float integral;
    float lastError;

    public PIDController(float p, float i, float d)
    {
        pFactor = p;
        iFactor = i;
        dFactor = d;
    }

    public float GetOutput(float currentError, float deltaTime)
    {
        integral += currentError * deltaTime;
        float derivative = (currentError - lastError) / deltaTime;
        lastError = currentError;

        return currentError * pFactor + integral * iFactor + derivative * dFactor;
    }
}
