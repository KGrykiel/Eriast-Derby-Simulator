using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Stage entryStage; // Assign this in the Inspector

    [Header("UI References (assign in Inspector)")]
    public TextMeshProUGUI statusNotesText;
    public Transform skillButtonContainer; 
    public Button skillButtonPrefab; 
    public GameObject targetSelectionPanel; 
    public Transform targetButtonContainer;
    public Button targetButtonPrefab;
    public Button targetCancelButton;
    public Button nextTurnButton;


    public GameObject stageSelectionPanel;
    public Transform stageButtonContainer;
    public Button stageButtonPrefab;


    private List<Stage> stages;
    private List<Vehicle> vehicles;
    private string statusText = "";
    private int currentTurnIndex = 0;
    public Vehicle playerVehicle; // Assign in Inspector or find in Start()

    private Vehicle selectedTarget = null;
    private bool isSelectingTarget = false;
    private Skill selectedSkill = null;
    private int selectedSkillIndex = -1;

    private bool isSelectingStage = false;
    private List<Stage> pendingStageChoices = new List<Stage>();

    void Start()
    {
        stages = new List<Stage>(FindObjectsByType<Stage>(FindObjectsSortMode.None));
        vehicles = new List<Vehicle>(FindObjectsByType<Vehicle>(FindObjectsSortMode.None));

        // The first vehicle with controlType == Player
        playerVehicle = vehicles.Find(v => v.controlType == ControlType.Player);

        // Roll initiative for each vehicle and store it in a dictionary
        Dictionary<Vehicle, int> initiativeRolls = new Dictionary<Vehicle, int>();
        foreach (var vehicle in vehicles)
        {
            int initiative = Random.Range(1, 101); // d100 roll instead of d20 because there will be a lot of vehicles
            initiativeRolls[vehicle] = initiative;
            SimulationLogger.LogEvent($"{vehicle.vehicleName} rolled initiative: {initiative}");
        }

        // Sort vehicles by initiative (descending)
        vehicles.Sort((a, b) => initiativeRolls[b].CompareTo(initiativeRolls[a]));
        SimulationLogger.LogEvent("Turn order: " + string.Join(", ", vehicles.ConvertAll(v => v.vehicleName)));

        // Initialize vehicle positions
        foreach (var vehicle in vehicles)
        {
            Stage startStage = entryStage != null ? entryStage : (stages.Count > 0 ? stages[0] : null);
            vehicle.progress = 0f;
            vehicle.SetCurrentStage(startStage);
            SimulationLogger.LogEvent($"{vehicle.vehicleName} placed at stage: {startStage?.stageName ?? "None"}");
        }
        if (targetCancelButton != null)
            targetCancelButton.onClick.AddListener(OnTargetCancelClicked);


        currentTurnIndex = 0;
        UpdateStatusText();
        NextTurn();
    }

    public void NextTurn()
    {
        if (vehicles.Count == 0) return;

        var vehicle = vehicles[currentTurnIndex];
        if (vehicle.currentStage == null)
        {
            SimulationLogger.LogEvent($"{vehicle.vehicleName} has no current stage. Skipping turn.");
            AdvanceTurn();
            NextTurn();
            return;
        }

        if (vehicle.Status == VehicleStatus.Destroyed)
        {
            SimulationLogger.LogEvent($"{vehicle.vehicleName} is not active. Skipping turn.");
            AdvanceTurn();
            NextTurn();
            return;
        }

        float speed = vehicle.GetAttribute(Attribute.Speed);
        vehicle.progress += speed;
        vehicle.UpdateModifiers();

        if (vehicle == playerVehicle)
        {
            ProcessPlayerMovement();
            return;
        }

        while (vehicle.progress >= vehicle.currentStage.length && vehicle.currentStage.nextStages.Count > 0)
        {
            vehicle.progress -= vehicle.currentStage.length;
            var options = vehicle.currentStage.nextStages;
            Stage nextStage = options[Random.Range(0, options.Count)];
            vehicle.SetCurrentStage(nextStage);
        }

        AdvanceTurn();
        UpdateStatusText();
        Invoke(nameof(NextTurn), 0f);
    }
    private void ProcessPlayerMovement()
    {
        // Only process if not already selecting a stage
        if (isSelectingStage) return;

        // Move as far as possible, but stop at crossroads for player choice
        while (playerVehicle.progress >= playerVehicle.currentStage.length)
        {
            if (playerVehicle.currentStage.nextStages.Count == 0)
            {
                // No next stage, stop
                break;
            }
            else if (playerVehicle.currentStage.nextStages.Count == 1)
            {
                // Only one way to go, move automatically
                playerVehicle.progress -= playerVehicle.currentStage.length;
                playerVehicle.SetCurrentStage(playerVehicle.currentStage.nextStages[0]);
            }
            else
            {
                // Crossroads: let the player choose
                isSelectingStage = true;
                pendingStageChoices = new List<Stage>(playerVehicle.currentStage.nextStages);
                ShowStageSelection(pendingStageChoices);
                return; // Wait for player input
            }
        }

        // After movement, show skills
        SimulationLogger.LogEvent($"It's now {playerVehicle.vehicleName}'s (Player) turn.");
        UpdateStatusText();
        ShowSkillSelection();
    }

    private void AdvanceTurn()
    {
        currentTurnIndex = (currentTurnIndex + 1) % vehicles.Count;
    }

    private void UpdateStatusText()
    {
        statusText = "";
        foreach (var vehicle in vehicles)
        {
            if (vehicle.currentStage == null) continue;
            statusText += $"{vehicle.vehicleName}: {vehicle.currentStage.stageName} ({vehicle.progress:0.0}/{vehicle.currentStage.length})\n";
        }
        if (statusNotesText != null)
            statusNotesText.text = statusText;
    }

    private List<Button> skillButtons = new List<Button>();

    private void ShowSkillSelection()
    {
        if (skillButtonContainer == null || skillButtonPrefab == null || playerVehicle == null) return;

        // Ensure enough buttons
        while (skillButtons.Count < playerVehicle.skills.Count)
        {
            Button btn = Instantiate(skillButtonPrefab, skillButtonContainer);
            skillButtons.Add(btn);
        }
        // Hide extra buttons
        for (int i = 0; i < skillButtons.Count; i++)
        {
            if (i < playerVehicle.skills.Count)
            {
                skillButtons[i].gameObject.SetActive(true);
                skillButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = playerVehicle.skills[i].name;
                int skillIndex = i;
                skillButtons[i].onClick.RemoveAllListeners();
                skillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(skillIndex));
            }
            else
            {
                skillButtons[i].gameObject.SetActive(false);
            }
        }
    }


    private void OnSkillButtonClicked(int skillIndex)
    {
        selectedSkill = playerVehicle.skills[skillIndex];
        selectedSkillIndex = skillIndex;
        SimulationLogger.LogEvent($"Player selected skill: {selectedSkill.name}");

        // Check if any effect in the skill needs a main target
        bool needsTarget = false;
        if (selectedSkill.effectInvocations != null)
        {
            foreach (var invocation in selectedSkill.effectInvocations)
            {
                if (invocation.targetMode == EffectTargetMode.Target || invocation.targetMode == EffectTargetMode.Both)
                {
                    needsTarget = true;
                    break;
                }
            }
        }

        if (needsTarget)
        {
            ShowTargetSelection();
        }
        else
        {
            isSelectingTarget = false;
            selectedTarget = null;
        }
    }


    private void ShowTargetSelection()
    {
        if (targetSelectionPanel == null || targetButtonContainer == null || targetButtonPrefab == null) return;

        targetSelectionPanel.SetActive(true);

        // Remove old buttons
        foreach (Transform child in targetButtonContainer)
            Destroy(child.gameObject);

        // Determine if a main target is needed and what the valid targets are
        List<Vehicle> validTargets = new List<Vehicle>();

        // Only show targets in the same stage, active, and not the player
        foreach (var v in vehicles)
        {
            if (v == playerVehicle) continue;
            if (v.currentStage != playerVehicle.currentStage) continue;
            if (v.Status != VehicleStatus.Active) continue;
            validTargets.Add(v);
        }

        // If no valid targets, show a message or just return
        if (validTargets.Count == 0)
        {
            SimulationLogger.LogEvent("No valid targets available.");
            if (targetSelectionPanel != null)
                targetSelectionPanel.SetActive(false);
            return;
        }

        foreach (var v in validTargets)
        {
            Button btn = Instantiate(targetButtonPrefab, targetButtonContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = v.vehicleName;
            btn.onClick.AddListener(() => OnTargetButtonClicked(v));
        }
    }


    private void OnTargetButtonClicked(Vehicle v)
    {
        selectedTarget = v;
        isSelectingTarget = false;
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);
        // Now you can use the skill or wait for another button
    }

    public void PlayPlayerSkillAndAdvance()
    {
        if (isSelectingStage) return; // Block if waiting for stage choice

        Vehicle target = playerVehicle; // Default to self

        if (selectedSkill != null)
        {
            // Determine if any effect in the skill needs a main target; self and AoE skills do not need a target
            bool needsTarget = false;
            if (selectedSkill.effectInvocations != null)
            {
                foreach (var invocation in selectedSkill.effectInvocations)
                {
                    if (invocation.targetMode == EffectTargetMode.Target || invocation.targetMode == EffectTargetMode.Both)
                    {
                        needsTarget = true;
                        break;
                    }
                }
            }

            // If a target is needed, ensure one is selected
            if (needsTarget)
            {
                target = selectedTarget;
                if (target == null)
                {
                    SimulationLogger.LogEvent("Player skipped their turn (no target selected).");
                    ClearPlayerSelections();
                    AdvanceTurn();
                    NextTurn();
                    return;
                }
            }

            // Check for enough energy
            if (playerVehicle.energy < selectedSkill.energyCost)
            {
                SimulationLogger.LogEvent($"{playerVehicle.vehicleName} does not have enough energy to use {selectedSkill.name}.");
                return;
            }

            // Use the skill
            bool result = selectedSkill.Use(playerVehicle, target);

            // For logging, try to show all main targets
            string targetName = GetSkillMainTargetName(selectedSkill, target);

            if (result)
            {
                playerVehicle.energy -= selectedSkill.energyCost;
                SimulationLogger.LogEvent($"Player used skill: {selectedSkill.name} on {targetName} (Success)");
            }
            else
            {
                SimulationLogger.LogEvent($"Player used skill: {selectedSkill.name} on {targetName} (Failed)");
            }
        }
        else
        {
            SimulationLogger.LogEvent("Player skipped their turn (no skill selected).");
        }

        ClearPlayerSelections();
        AdvanceTurn();
        NextTurn();
    }

    // Helper to get a display name for the main target (for logging)
    private string GetSkillMainTargetName(Skill skill, Vehicle mainTarget)
    {
        if (skill.effectInvocations != null && skill.effectInvocations.Exists(ei => ei.targetMode == EffectTargetMode.AllInStage))
            return "all vehicles in stage";
        if (skill.effectInvocations != null && skill.effectInvocations.Exists(ei => ei.targetMode == EffectTargetMode.Both))
            return $"{playerVehicle.vehicleName} and {mainTarget.vehicleName}";
        return mainTarget != null ? mainTarget.vehicleName : "none";
    }



    private void ClearPlayerSelections()
    {
        selectedSkill = null;
        selectedSkillIndex = -1;
        selectedTarget = null;
        isSelectingTarget = false;
    }
    private void OnTargetCancelClicked()
    {
        isSelectingTarget = false;
        selectedTarget = null;
        selectedSkill = null;
        selectedSkillIndex = -1;
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);
    }

    // These are now just pass-throughs to the logger or data
    public IReadOnlyList<string> GetSimulationLog() => SimulationLogger.Log;
    public List<Vehicle> GetVehicles() => vehicles;
    private List<Button> stageButtons = new List<Button>();

    private void ShowStageSelection(List<Stage> options)
    {
        if (stageSelectionPanel == null || stageButtonContainer == null || stageButtonPrefab == null) return;

        stageSelectionPanel.SetActive(true);
        if (nextTurnButton != null)
            nextTurnButton.interactable = false;

        // Ensure enough buttons
        while (stageButtons.Count < options.Count)
        {
            Button btn = Instantiate(stageButtonPrefab, stageButtonContainer);
            stageButtons.Add(btn);
        }

        // Set up and show only as many as needed
        for (int i = 0; i < stageButtons.Count; i++)
        {
            if (i < options.Count)
            {
                stageButtons[i].gameObject.SetActive(true);
                stageButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = options[i].stageName;
                Stage stage = options[i]; // Capture for closure
                stageButtons[i].onClick.RemoveAllListeners();
                stageButtons[i].onClick.AddListener(() => OnStageButtonClicked(stage));
            }
            else
            {
                stageButtons[i].gameObject.SetActive(false);
            }
        }
    }


    private void OnStageButtonClicked(Stage selectedStage)
    {
        stageSelectionPanel.SetActive(false);
        isSelectingStage = false;
        pendingStageChoices.Clear();

        if (nextTurnButton != null)
            nextTurnButton.interactable = true;

        // Move the player vehicle to the selected stage
        playerVehicle.progress -= playerVehicle.currentStage.length;
        playerVehicle.SetCurrentStage(selectedStage);

        // Continue processing movement if needed
        ProcessPlayerMovement();
    }


}
