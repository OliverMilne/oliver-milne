using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovementScript : MonoBehaviour
{
    public KeyCode MoveUpKey;
    public KeyCode MoveDownKey;
    public KeyCode MoveLeftKey;
    public KeyCode MoveRightKey;

    private float _movementSpeed = 0.1f;

    private float _upperBound 
    {
        get => CurrentGameState.Instance.gameStateInfo.cameraMovementData.upperBound;
        set { CurrentGameState.Instance.gameStateInfo.cameraMovementData.upperBound = value; }
    }
    private float _lowerBound 
    {
        get => CurrentGameState.Instance.gameStateInfo.cameraMovementData.lowerBound;
        set { CurrentGameState.Instance.gameStateInfo.cameraMovementData.lowerBound = value; }
    }
    private float _leftBound
    {
        get => CurrentGameState.Instance.gameStateInfo.cameraMovementData.leftBound;
        set { CurrentGameState.Instance.gameStateInfo.cameraMovementData.leftBound = value; }
    }
private float _rightBound
    {
        get => CurrentGameState.Instance.gameStateInfo.cameraMovementData.rightBound;
        set { CurrentGameState.Instance.gameStateInfo.cameraMovementData.rightBound = value; }
    }

    public void SetBounds(float upper, float lower, float left, float right)
    {
        _upperBound = upper;
        _lowerBound = lower;
        _leftBound = left;
        _rightBound = right;
    }

    public void SetCameraPosition(float x, float y)
    {
        GetComponent<Transform>().position = new Vector3(x, y, GetComponent<Transform>().position.z);
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(MoveUpKey) && GetComponent<Transform>().position.y < _upperBound)
        {
            GetComponent<Transform>().Translate(new Vector3(0, _movementSpeed, 0));
        }
        if (Input.GetKey(MoveDownKey) && GetComponent<Transform>().position.y > _lowerBound)
        {
            GetComponent<Transform>().Translate(new Vector3(0, -_movementSpeed, 0));
        }
        if (Input.GetKey(MoveLeftKey) && GetComponent<Transform>().position.x > _leftBound)
        {
            GetComponent<Transform>().Translate(new Vector3(-_movementSpeed, 0, 0));
        }
        if (Input.GetKey(MoveRightKey) && GetComponent<Transform>().position.x < _rightBound)
        {
            GetComponent<Transform>().Translate(new Vector3(_movementSpeed, 0, 0));
        }

        // save camera position to GameStateInfo
        CurrentGameState.Instance.gameStateInfo.cameraMovementData.cameraPosition[0] 
            = GetComponent<Transform>().position.x;
        CurrentGameState.Instance.gameStateInfo.cameraMovementData.cameraPosition[1]
            = GetComponent<Transform>().position.y;
        CurrentGameState.Instance.gameStateInfo.cameraMovementData.cameraPosition[2]
            = GetComponent<Transform>().position.z;
    }
}

public class CameraMovementData
{
    public float[] cameraPosition = new float[3];

    public float upperBound = 99999f;
    public float lowerBound = -99999f;
    public float leftBound = -99999f;
    public float rightBound = 99999f;
}
