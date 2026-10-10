using UnityEngine;

public class Car : InteractableManager
{
    public Generator generator;
    public bool isConeectedToCar = false;
    public GameObject lineRenderer;

    public override void Interact(PlayerManager player)
    {
        if (isConeectedToCar)
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Car is already connected with a fuel line.");
            ShowThePipeline();
            return;
        }

        if (PlayerSelector.Instance.currentInventory &&
            PlayerSelector.Instance.currentInventory.currentSelectedItem is OilPipeline)
        {
            isConeectedToCar = true;

            if (generator && generator.isConnectedWithFuel)
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Fuel line connected. Click the generator to start it.");
            }
            else
            {
                PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("Car is now connected with fuel. Now connect the generator with a fuel line.");
            }
        }
        else
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow("You need to select a fuel line to connect the car with the generator.");
        }

        ShowThePipeline();
    }

    public void ShowThePipeline()
    {
        if (lineRenderer)
        {
            lineRenderer.SetActive(isConeectedToCar && generator && generator.isConnectedWithFuel);
        }
    }
}
