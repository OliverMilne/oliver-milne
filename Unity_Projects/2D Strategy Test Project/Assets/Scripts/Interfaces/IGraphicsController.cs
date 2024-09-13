using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGraphicsController
{
    public void ApplySprite();
    public void HideSprite();
    public void ShowSprite();
    public void ToggleSpriteVisibility();
}
