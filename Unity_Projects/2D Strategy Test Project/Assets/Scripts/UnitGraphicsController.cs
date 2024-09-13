using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This controls unit graphics. It's attached to the unit GameObject.
/// </summary>
public class UnitGraphicsController : MonoBehaviour, IGraphicsController
{
    private GameObject _playerColourSpriteObject;
    private LineRenderer _unitBarBackgroundLineRenderer;
    private LineRenderer _unitHealthBarLineRenderer;
    private LineRenderer _unitReadinessBarLineRenderer;
    private SpriteRenderer _baseSpriteRenderer;
    private SpriteRenderer _playerColourSpriteRenderer;
    private UnitInfo _unitInfo;

    [SerializeField]
    public bool _isVisible = true;

    private void Awake()
    {
        ApplySprite();
    }

    public void ApplySprite()
    {
        GetValues();
        // Debug.Log("ApplySprite called on unit " + _unitInfo.unitInfoID + ": " + _unitInfo.UnitInfoString());
        _baseSpriteRenderer.sprite = _unitInfo.UnitSprite;
        _playerColourSpriteRenderer.sprite = _unitInfo.UnitPlayerColourSprite;

        PlayerProperties owningPlayer = 
            PlayerSetupScript.Instance.playerList.Find(x => x.playerID == _unitInfo.ownerID);
        if (owningPlayer != null) _playerColourSpriteRenderer.color = owningPlayer.playerColor;
        // Debug.Log("spriteRenderer sprite for locatableObject " + locatableID + " is " + spriteRenderer.sprite);
    }
    private void GetValues()
    {
        if (_unitBarBackgroundLineRenderer == null)
            _unitBarBackgroundLineRenderer 
                = transform.Find("BarBackgroundObject").GetComponent<LineRenderer>();
        if (_unitHealthBarLineRenderer == null) _unitHealthBarLineRenderer = GetComponent<LineRenderer>();
        if (_unitInfo == null) _unitInfo = GetComponent<UnitInfo>();
        if (_unitReadinessBarLineRenderer == null) 
            _unitReadinessBarLineRenderer 
                = transform.Find("ReadinessBarObject").GetComponent<LineRenderer>();
        if (_baseSpriteRenderer == null) _baseSpriteRenderer = GetComponent<SpriteRenderer>();

        if (_playerColourSpriteObject == null)
            _playerColourSpriteObject = transform.Find("PlayerColourSpriteObject").gameObject;
        if (_playerColourSpriteRenderer == null)
            _playerColourSpriteRenderer = _playerColourSpriteObject.GetComponent<SpriteRenderer>();

    }
    public void HideSprite() 
    { 
        GetValues();
        _isVisible = false;
        // Debug.Log("HideSprite called on unit " + _unitInfo.unitInfoID);

        _baseSpriteRenderer.forceRenderingOff = true;
        _playerColourSpriteRenderer.forceRenderingOff = true;
        HideUnitHealthBar();
        HideUnitReadinessBar();
        HideUnitBarBackground();
    }
    public void HideUnitBarBackground()
    {
        if (_unitBarBackgroundLineRenderer == null)
            _unitBarBackgroundLineRenderer = transform.Find("BarBackgroundObject").GetComponent<LineRenderer>();
        _unitBarBackgroundLineRenderer.enabled = false;
    }
    public void HideUnitHealthBar()
    {
        if (_unitHealthBarLineRenderer == null) _unitHealthBarLineRenderer = GetComponent<LineRenderer>();
        _unitHealthBarLineRenderer.enabled = false;
    }
    public void HideUnitReadinessBar()
    {
        if (_unitReadinessBarLineRenderer == null) 
            _unitReadinessBarLineRenderer = transform.Find("ReadinessBarObject").GetComponent<LineRenderer>();
        _unitReadinessBarLineRenderer.enabled = false;
    }
    public void RenderUnitBarBackground()
    {
        if (_unitBarBackgroundLineRenderer == null)
            _unitBarBackgroundLineRenderer 
                = transform.Find("BarBackgroundObject").GetComponent<LineRenderer>();
        if (_isVisible) _unitBarBackgroundLineRenderer.enabled = true;

        _unitBarBackgroundLineRenderer.startColor = new Color(0,0,0,0.3f);
        _unitBarBackgroundLineRenderer.startWidth = 0.2f;
        Vector3 barStartPosition = new Vector3(
            transform.position.x - 0.32f, transform.position.y - 0.35f, transform.position.z - 0.9f);
        Vector3 barEndPosition = new Vector3(
            (transform.position.x - 0.3f) + (0.62f),
            transform.position.y - 0.35f,
            transform.position.z - 0.9f);
        _unitBarBackgroundLineRenderer.SetPositions(new Vector3[] { barStartPosition, barEndPosition });
    }
    public void RenderUnitHealthBar()
    {
        if (_unitHealthBarLineRenderer == null)
            _unitHealthBarLineRenderer = GetComponent<LineRenderer>();
        if (_unitInfo == null)
            _unitInfo = GetComponent<UnitInfo>();

        if (_isVisible) _unitHealthBarLineRenderer.enabled = true;
        float unitHealthRatio = (float)_unitInfo.hitpoints / (float)Mathf.Max(1, _unitInfo.maxHP);

        _unitHealthBarLineRenderer.startWidth = 0.08f;
        Vector3 barStartPosition = new Vector3(
            transform.position.x-0.3f, transform.position.y-0.3f,transform.position.z-1);
        Vector3 barEndPosition = new Vector3(
            (transform.position.x - 0.3f) + (0.6f * unitHealthRatio), 
            transform.position.y - 0.3f, 
            transform.position.z - 1);
        _unitHealthBarLineRenderer.SetPositions(new Vector3[]{ barStartPosition, barEndPosition});
    }
    public void RenderUnitReadinessBar()
    {
        if (_unitReadinessBarLineRenderer == null)
            _unitReadinessBarLineRenderer = transform.Find("ReadinessBarObject").GetComponent<LineRenderer>();
        if (_unitInfo == null)
            _unitInfo = GetComponent<UnitInfo>();

        if (_isVisible) _unitReadinessBarLineRenderer.enabled = true;

        _unitReadinessBarLineRenderer.startWidth = 0.08f;
        Vector3 barStartPosition = new Vector3(
            transform.position.x - 0.3f, transform.position.y - 0.4f, transform.position.z - 1);
        Vector3 barEndPosition = new Vector3(
            (transform.position.x - 0.3f) + (0.6f * _unitInfo.currentReadiness),
            transform.position.y - 0.4f,
            transform.position.z - 1);
        _unitReadinessBarLineRenderer.SetPositions(new Vector3[] { barStartPosition, barEndPosition });
    }
    public void ShowSprite() 
    {
        GetValues();
        _isVisible = true;
        // Debug.Log("ShowSprite called on unit " + _unitInfo.unitInfoID);

        _baseSpriteRenderer.forceRenderingOff = false;
        _playerColourSpriteRenderer.forceRenderingOff = false;
        RenderUnitBarBackground();
        RenderUnitHealthBar();
        RenderUnitReadinessBar();
    }
    public void ToggleSpriteVisibility()
    {
        if (_isVisible) HideSprite();
        else ShowSprite();
    }
}
