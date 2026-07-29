using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ItemSocketPuzzleUIController : MazePuzzleUIControllerBase
{
    private const int SlotCount = ItemSocketPuzzleSystem.SlotCount;
    private const float ClickShrinkDuration = 0.15f;
    private const float ClickRestoreDuration = 0.075f;
    private const float ClickMinScale = 0.8f;
    private const float ClickTickInterval = 0.001f;

    private readonly List<Button> slotButtons = new List<Button>();
    private readonly List<Text> slotTexts = new List<Text>();
    private readonly List<Button> itemButtons = new List<Button>();
    private readonly List<Text> itemTexts = new List<Text>();
    // 每个按钮当前在跑的点击动画 timerId，用于连点时取消上一次。
    private readonly Dictionary<Transform, int> clickAnimations = new Dictionary<Transform, int>();
    private Button removeButton;

    protected override string ExpectedPuzzleType => ItemSocketPuzzleSystem.TypeId;
    protected override string DefaultTitle => "解密一 · 圆坛置物";

    protected override void BindPuzzleUI()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            slotButtons.Add(FindButton("SlotButton_" + i));
            slotTexts.Add(FindText("SlotText_" + i));
            itemButtons.Add(FindButton("ItemButton_" + i));
            itemTexts.Add(FindText("ItemText_" + i));
        }
        removeButton = FindButton("RemoveButton");
    }

    protected override void MapPuzzleEvents()
    {
        for (int i = 0; i < itemButtons.Count; i++)
        {
            int index = i;
            Button btn = itemButtons[i];
            BindClickEvent(btn, () => {
                // 先播动画：Select 会触发重绘，可能把按钮 SetActive(false)。
                PlayClickAnimation(btn);
                Select(index);
                });
        }
        // 点已填充的槽位＝把那件道具取回托盘，省掉"清空重摆"的来回。
        for (int i = 0; i < slotButtons.Count; i++)
        {
            int index = i;
            BindClickEvent(slotButtons[i], () => ClearSlot(index));
        }
        BindClickEvent(removeButton, ClearAll);
    }

    protected override void RenderPuzzle(MazePuzzleRoomViewModel vm)
    {
        for (int i = 0; i < itemButtons.Count; i++)
            SetOptionButton(itemButtons[i], itemTexts[i], i < vm.options.Count ? vm.options[i] : null, vm.isSolved);

        RenderSlots(vm);
        if (removeButton != null)
            removeButton.interactable = !vm.isSolved && HasAnyPlaced(vm);
    }

    private void RenderSlots(MazePuzzleRoomViewModel vm)
    {
        for (int i = 0; i < slotButtons.Count; i++)
        {
            Button slot = slotButtons[i];
            if (slot == null)
                continue;

            string placed = i < vm.slotItemKeys.Count ? vm.slotItemKeys[i] : string.Empty;
            bool filled = !string.IsNullOrEmpty(placed);
            UIHelper.SetText(slotTexts[i], filled ? ResolvePlacedLabel(vm, placed) : "+");
            // 空槽不接受点击：摆放入口是托盘按钮，槽位只负责取回。
            slot.interactable = filled && !vm.isSolved;
            Image image = slot.GetComponent<Image>();
            if (image != null)
                image.color = filled
                    ? new Color(0.85f, 0.72f, 0.25f, 1f)
                    : new Color(0.32f, 0.38f, 0.46f, 1f);
        }
    }

    private static bool HasAnyPlaced(MazePuzzleRoomViewModel vm)
    {
        for (int i = 0; i < vm.slotItemKeys.Count; i++)
        {
            if (!string.IsNullOrEmpty(vm.slotItemKeys[i]))
                return true;
        }
        return false;
    }

    private static string ResolvePlacedLabel(MazePuzzleRoomViewModel vm, string key)
    {
        for (int i = 0; i < vm.options.Count; i++)
        {
            if (vm.options[i] != null && vm.options[i].key == key)
                return vm.options[i].label;
        }
        return key;
    }

    private void Select(int index)
    {
        if (commands == null || viewModel == null || index < 0 || index >= viewModel.options.Count)
            return;
        Execute(commands.Execute(new SelectItemSocketPuzzleItemCommand(viewModel.puzzleId, viewModel.options[index].key)));
    }

    private void ClearSlot(int slotIndex)
    {
        if (commands != null && viewModel != null)
            Execute(commands.Execute(new RemoveItemSocketPuzzleItemCommand(viewModel.puzzleId, slotIndex)));
    }

    private void ClearAll()
    {
        if (commands != null && viewModel != null)
            Execute(commands.Execute(new RemoveItemSocketPuzzleItemCommand(viewModel.puzzleId)));
    }

    // 控制器不是 MonoBehaviour，开不了协程；借 GameTimer 逐帧驱动缩放反馈。
    // GameTimer 把间隔夹到最小 0.001s 且每次 Tick 只触发一次，等价于每帧回调一次。
    private void PlayClickAnimation(Button btn)
    {
        if (btn == null || FrameworkContext.Instance == null)
            return;

        GameTimer timer = FrameworkContext.Instance.Timer;
        Transform target = btn.transform;
        float elapsed = 0f;
        int timerId = 0;

        // 连点时取消上一次动画，避免多个 timer 抢写同一个 localScale。
        if (clickAnimations.TryGetValue(target, out int running))
            timer.Cancel(running);

        timerId = timer.ScheduleRepeating(ClickTickInterval, () =>
        {
            // 界面关闭后按钮已销毁，target 变成 Unity 的 fake null，动画在这里自行收尾。
            if (target == null)
            {
                timer.Cancel(timerId);
                clickAnimations.Remove(target);
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed >= ClickShrinkDuration + ClickRestoreDuration)
            {
                target.localScale = Vector3.one;
                timer.Cancel(timerId);
                clickAnimations.Remove(target);
                return;
            }

            float scale = elapsed < ClickShrinkDuration
                ? 1f - elapsed / ClickShrinkDuration * (1f - ClickMinScale)
                : ClickMinScale + (elapsed - ClickShrinkDuration) / ClickRestoreDuration * (1f - ClickMinScale);
            target.localScale = Vector3.one * scale;
        });
        clickAnimations[target] = timerId;
    }

    public override void Dismiss()
    {
        GameTimer timer = FrameworkContext.Instance?.Timer;
        if (timer != null)
        {
            foreach (KeyValuePair<Transform, int> kv in clickAnimations)
            {
                timer.Cancel(kv.Value);
                // 复原缩放，避免按钮被复用时残留在中间状态。
                if (kv.Key != null)
                    kv.Key.localScale = Vector3.one;
            }
        }
        clickAnimations.Clear();
        base.Dismiss();
    }
}
