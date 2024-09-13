using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionInfoDisplayScript : MonoBehaviour
{
    // This should be passed a SelectableObject by the UIControlScript, and autoupdate the display
    // every time its relevant info changes.
    public SelectableObject SelectedObject
    {
        get => _selectedObjectBehind;
        set
        {
            // unsubscribe from existing value's OnDisplayableInfoUpdated event
            if (_selectedObjectBehind != null) 
            {
                if (_selectedObjectBehind.locatableObject.isUnit) 
                {
                    UnitInfo selectedUnitInfo = _selectedObjectBehind.GetComponent<UnitInfo>();
                    selectedUnitInfo.OnDisplayableInfoUpdated -= RefreshInfoDisplay;
                }  
            }
            _selectedObjectBehind = value;
            if (value != null)
            {
                _SelectionDisplayText.text = "Selected: " + value.ReturnSelectionInfo();
                // subscribe to new value's OnDisplayableInfoUpdated event
                if (value.locatableObject.isUnit)
                {
                    UnitInfo selectedUnitInfo = value.GetComponent<UnitInfo>();
                    selectedUnitInfo.OnDisplayableInfoUpdated += RefreshInfoDisplay;
                }
            }
            else { _SelectionDisplayText.text = "Nothing selected"; }
        }
    }
    private SelectableObject _selectedObjectBehind;
    private Text _SelectionDisplayText
    {
        get
        {
            if (_selectionDisplayTextBehind == null)
                _selectionDisplayTextBehind 
                    = transform.Find("Selection Info Text").GetComponent<Text>();
            return _selectionDisplayTextBehind;
        }
    }
    private Text _selectionDisplayTextBehind;

    private void RefreshInfoDisplay()
    {
        SelectedObject = SelectedObject;
    }
}
