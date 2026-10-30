using UnityEngine;
using UnityEngine.UI;

namespace FrogSea
{
    // Serialized references and copy remain editable in the scene or TitleScreen prefab.
    public sealed class TitleView : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] GameObject home;
        [SerializeField] GameObject selection;
        [Header("Rule details")]
        [SerializeField] Text ruleName;
        [SerializeField] Text description;
        [SerializeField] Text stageLabel;
        [SerializeField] Button previous;
        [SerializeField] Button next;
        [Header("Editable rule copy")]
        [SerializeField] string stagesTitle = "STAGES / Find the sea";
        [SerializeField] string endlessTitle = "ENDLESS / Keep climbing";
        [TextArea, SerializeField] string stagesDescription = "Three wells. Reach the surface to see the sea.\nA missed jump can send you back down.";
        [TextArea, SerializeField] string endlessDescription = "An endless well with new ledges above.\nYour highest climb is saved on this device.";
        public void Render(TitleModel model)
        {
            home.SetActive(!model.ChoosingRules); selection.SetActive(model.ChoosingRules);
            bool stages = model.Mode == GameMode.Stages;
            ruleName.text = stages ? stagesTitle : endlessTitle;
            description.text = stages ? stagesDescription : endlessDescription;
            previous.gameObject.SetActive(stages); next.gameObject.SetActive(stages); stageLabel.gameObject.SetActive(stages);
            stageLabel.text = "Stage " + model.Stage + "  /  " + (30 + model.Stage * 12) + " m";
            previous.interactable = model.Stage > 1; next.interactable = model.Stage < 3;
        }
    }
}
