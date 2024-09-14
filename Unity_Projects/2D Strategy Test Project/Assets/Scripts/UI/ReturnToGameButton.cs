using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnToGameButton : MonoBehaviour
{
    public void OnButtonClick()
    {
        transform.parent.gameObject.SetActive(false);
    }
}
