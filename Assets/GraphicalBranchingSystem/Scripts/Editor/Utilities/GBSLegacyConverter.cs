using System.Collections.Generic;
using GBS.Data;
using GBS.Steps;
using Game.Scripts.Instructions.Story;
using Game.Scripts.Story;
using Game.Scripts.Story.EditorTools;
using UnityEditor;
using UnityEngine.Playables;

namespace GBS.Utility
{
    /// <summary>
    /// Перенос старого SO-контента сюжета во встроенные шаги графа:
    ///  - "параметрические" ассеты (подсказка, задержка, кнопка, камера, таймлайн, флаги, AllOf/AnyOf)
    ///    становятся встроенными шагами с теми же настройками - ассет больше не нужен;
    ///  - SO-каналы становятся ключами состояния сюжета "Legacy/&lt;имя ассета&gt;": старые каналы и
    ///    слушатели на сцене продолжают работать через мосты (StoryEventChannelSO.Raise поднимает
    ///    сигнал, StoryEventChannelListener отвечает как реакция сцены), ключи добавляются в каталог;
    ///  - остальное (свои наследники StoryAction/StoryCondition) - обёртки Legacy*Asset.
    /// Используется импортёром StoryNodeSO и при открытии графов до v3.0.
    /// </summary>
    public static class GBSLegacyConverter
    {
        private const string LegacyKeyDescription = "Мост со старого SO-ассета: ";

        public static void UpgradeStoryNode(GBSStoryNodeData node)
        {
            if (node.HasLegacyContent)
            {
                var converted = new List<GBSAction>();

                foreach (var legacyEvent in node.LegacyEvents)
                {
                    if (legacyEvent != null)
                    {
                        converted.Add(new LegacyGBSEventAsset(legacyEvent));
                    }
                }

                foreach (var legacyAction in node.LegacyActions)
                {
                    var action = ConvertAction(legacyAction);

                    if (action != null)
                    {
                        converted.Add(action);
                    }
                }

                node.Actions.InsertRange(0, converted);
                node.ClearLegacyContent();
            }

            foreach (var branch in node.Branches)
            {
                if (branch.LegacyCondition == null)
                {
                    continue;
                }

                branch.InlineCondition ??= ConvertCondition(branch.LegacyCondition);
                branch.LegacyCondition = null;
            }
        }

        public static void UpgradeConditionNode(GBSConditionNodeData node)
        {
            if (node.LegacyCondition == null)
            {
                return;
            }

            node.InlineCondition ??= ConvertCondition(node.LegacyCondition);
            node.LegacyCondition = null;
        }

        public static void UpgradeEndNode(GBSEndNodeData node)
        {
            if (node.LegacyEvents.Count == 0)
            {
                return;
            }

            var converted = new List<GBSAction>();

            foreach (var legacyEvent in node.LegacyEvents)
            {
                if (legacyEvent != null)
                {
                    converted.Add(new LegacyGBSEventAsset(legacyEvent));
                }
            }

            node.OnCompleteActions.InsertRange(0, converted);
            node.ClearLegacyContent();
        }

        public static GBSAction ConvertAction(StoryAction asset)
        {
            if (asset == null)
            {
                return null;
            }

            using var serialized = new SerializedObject(asset);

            switch (asset)
            {
                case ShowHintAction:
                    return new ShowHint(
                        ReadString(serialized, "m_Text"),
                        ReadFloat(serialized, "m_Duration"),
                        ReadBool(serialized, "m_WaitUntilHidden"));

                case SetPlayerControlAction:
                    return new SetPlayerControl(ReadBool(serialized, "m_IsEnabled"));

                case CameraBlendAction:
                    return new BlendCamera(ReadString(serialized, "m_TargetCameraKey"), ReadFloat(serialized, "m_FadeDuration"));

                case PlayTimelineAction:
                    return new PlayTimeline(
                        serialized.FindProperty("m_Timeline")?.objectReferenceValue as PlayableAsset,
                        ReadBool(serialized, "m_WaitForCompletion"));

                case SetBlackboardFlagAction:
                {
                    var key = ReadString(serialized, "m_Key");
                    StoryKeyCatalogUtility.AddKey(key, StoryKeyKind.Flag);
                    return new SetFlag(key, ReadBool(serialized, "m_Value"));
                }

                case RaiseEventChannelAction:
                {
                    var channel = serialized.FindProperty("m_Channel")?.objectReferenceValue;
                    return channel != null ? new RaiseSignal(LegacyKey(channel, StoryKeyKind.Signal)) : null;
                }

                case UnityEventAction:
                    // Слушатель StoryEventChannelListener на сцене отвечает на этот ключ как реакция сцены.
                    return new InvokeSceneReaction(LegacyKey(asset, StoryKeyKind.Reaction));

                case MarkAudioTapeCompletedAction markTape:
                    return new MarkAudioTapeCompleted(markTape.Tape);

                default:
                    return new LegacyStoryActionAsset(asset);
            }
        }

        public static GBSCondition ConvertCondition(StoryCondition asset)
        {
            if (asset == null)
            {
                return null;
            }

            using var serialized = new SerializedObject(asset);

            switch (asset)
            {
                case AlwaysTrueCondition:
                    return new AlwaysTrue();

                case DelayCondition:
                    return new AfterDelay(ReadFloat(serialized, "m_Seconds"));

                case InputButtonCondition:
                    return new InputPressed((StoryInputButton)(serialized.FindProperty("m_Button")?.intValue ?? 0));

                case StoryEventCondition:
                {
                    var channel = serialized.FindProperty("m_EventChannel")?.objectReferenceValue;
                    return channel != null
                        ? new SignalRaised(LegacyKey(channel, StoryKeyKind.Signal), true)
                        : new AlwaysTrue();
                }

                case BoolChannelCondition:
                {
                    var channel = serialized.FindProperty("m_Channel")?.objectReferenceValue;
                    return channel != null
                        ? new FlagIs(LegacyKey(channel, StoryKeyKind.Flag), ReadBool(serialized, "m_ExpectedValue"))
                        : new AlwaysTrue();
                }

                case BlackboardFlagCondition:
                {
                    var key = ReadString(serialized, "m_Key");
                    StoryKeyCatalogUtility.AddKey(key, StoryKeyKind.Flag);
                    return new FlagIs(key, ReadBool(serialized, "m_ExpectedValue"));
                }

                case AllOfCondition:
                    return new AllOf(ConvertConditions(serialized));

                case AnyOfCondition:
                    return new AnyOf(ConvertConditions(serialized));

                default:
                    return new LegacyStoryConditionAsset(asset);
            }
        }

        private static List<GBSCondition> ConvertConditions(SerializedObject serialized)
        {
            var result = new List<GBSCondition>();
            var array = serialized.FindProperty("m_Conditions");

            if (array == null || !array.isArray)
            {
                return result;
            }

            for (int i = 0; i < array.arraySize; i++)
            {
                var converted = ConvertCondition(array.GetArrayElementAtIndex(i).objectReferenceValue as StoryCondition);

                if (converted != null)
                {
                    result.Add(converted);
                }
            }

            return result;
        }

        private static string LegacyKey(UnityEngine.Object asset, StoryKeyKind kind)
        {
            var key = StorySignals.LegacyKey(asset);
            StoryKeyCatalogUtility.AddKey(key, kind, LegacyKeyDescription + AssetDatabase.GetAssetPath(asset));
            return key;
        }

        private static string ReadString(SerializedObject serialized, string name)
        {
            return serialized.FindProperty(name)?.stringValue;
        }

        private static float ReadFloat(SerializedObject serialized, string name)
        {
            return serialized.FindProperty(name)?.floatValue ?? 0f;
        }

        private static bool ReadBool(SerializedObject serialized, string name)
        {
            return serialized.FindProperty(name)?.boolValue ?? false;
        }
    }
}
