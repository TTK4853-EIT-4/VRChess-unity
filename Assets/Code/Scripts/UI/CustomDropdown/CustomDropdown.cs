using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomDropdown : MonoBehaviour
{
    // On click
    public void OnClick()
    {
        Debug.Log("Dropdown clicked");
        // Get the dropdown
        GameObject dropdown = GameObject.Find("Dropdown");
        // Get the dropdown list
        GameObject dropdownList = GameObject.Find("DropdownList");
        // Get the dropdown list items
        GameObject[] dropdownListItems = GameObject.FindGameObjectsWithTag("DropdownListItem");
        // Get the dropdown list items container
        GameObject dropdownListItemsContainer = GameObject.Find("DropdownListItemsContainer");

        // If the dropdown list is active
        if (dropdownList.activeSelf)
        {
            // Hide the dropdown list
            dropdownList.SetActive(false);
        }
        else
        {
            // Show the dropdown list
            dropdownList.SetActive(true);
        }
    }
}
