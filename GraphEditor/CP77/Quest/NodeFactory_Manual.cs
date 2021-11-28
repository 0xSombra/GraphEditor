using System;
using WolvenKit.Common.Model.Cr2w;
using CP77Types = WolvenKit.RED4.CR2W.Types;

// questLogicalHubNodeDefinition defaults to 1 input socket
// 'sq025_07_workshop.questphase' somehow use 2 default input sockets
// 
// 'sq027_queen_of_the_highway.questphase' has inputsockets set to 1 but it somehow has 2 input sockets... what
// assuming default input socket count to be 2 (this will add sockets to some nodes but that doesnt matter)

// questRandomizerNodeDefinition has no default num of out sockets
// yet some files somehow use 2 default out sockets
// assuming default output socket count to be 2

namespace GraphEditor.CP77.Quest
{
    using AddSocketFn = Action<string /* name */, CP77Types.Enums.questSocketType /* socketType */>;

    static partial class NodeFactory
    {
        static void AddNodeSockets_Default(IEditableVariable _, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);
            addInputSocket("In", CP77Types.Enums.questSocketType.Input);
            addOutputSocket("Out", CP77Types.Enums.questSocketType.Output);
        }

        public static void AddNodeSockets_scnSceneResource(CP77Types.scnSceneResource obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);

            foreach (var entryPoint in obj.EntryPoints)
            {
                addInputSocket(entryPoint.Name.Value, CP77Types.Enums.questSocketType.Input);
            }

            foreach (var exitPoint in obj.ExitPoints)
            {
                addOutputSocket(exitPoint.Name.Value, CP77Types.Enums.questSocketType.Output);
            }

            foreach (var interruption in obj.InterruptionScenarios)
            {
                addOutputSocket($"{interruption.Name.Value} INT", CP77Types.Enums.questSocketType.Output);
                addOutputSocket($"{interruption.Name.Value} RET", CP77Types.Enums.questSocketType.Output);
            }

            addInputSocket("Prefetch", CP77Types.Enums.questSocketType.Input);
        }

        public static void AddNodeSockets_questPhaseNodeDefinition(CP77Types.questPhaseNodeDefinition obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            var phaseGraph = obj.PhaseGraph.GetInstance();
            if (phaseGraph == null)
            {
                addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);
            }
            else
            {
                AddNodeSockets_questGraphDefinition(phaseGraph, addInputSocket, addOutputSocket);
            }
        }

        public static void AddNodeSockets_questGraphDefinition(CP77Types.questGraphDefinition obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);
            foreach (var nodeHandle in obj.Nodes)
            {
                var node = nodeHandle.GetInstance();
                if (node is CP77Types.questInputNodeDefinition inputNode)
                    addInputSocket(inputNode.SocketName.Value, CP77Types.Enums.questSocketType.Input);
                else if (node is CP77Types.questOutputNodeDefinition outputNode)
                    addOutputSocket(outputNode.SocketName.Value, CP77Types.Enums.questSocketType.Output);
            }
        }

        public static void AddNodeSockets_questCombatNodeDefinition(CP77Types.questCombatNodeDefinition _, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);
            addInputSocket("In", CP77Types.Enums.questSocketType.Input);
            addOutputSocket("Success", CP77Types.Enums.questSocketType.Output);
        }

        public static void AddNodeSockets_questMiscAICommandNode(CP77Types.questMiscAICommandNode _, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);
            addInputSocket("In", CP77Types.Enums.questSocketType.Input);
            addOutputSocket("Success", CP77Types.Enums.questSocketType.Output);
        }

        public static void AddNodeSockets_questMovePuppetNodeDefinition(CP77Types.questMovePuppetNodeDefinition _, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);
            addInputSocket("In", CP77Types.Enums.questSocketType.Input);
            addOutputSocket("Out", CP77Types.Enums.questSocketType.Output);
        }

        public static void AddNodeSockets_questLogicalBaseNodeDefinition(CP77Types.questLogicalBaseNodeDefinition obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);

            var inputSockets = /*obj is CP77Types.questLogicalHubNodeDefinition ? 1u : */2u;
            if (obj.InputSocketCount.Value > inputSockets)
                inputSockets = Math.Min(obj.InputSocketCount.Value, 32u);
            for (var i = 0; i != inputSockets; ++i)
            {
                addInputSocket($"In{i + 1}", CP77Types.Enums.questSocketType.Input);
            }

            var outputSockets = 1u;
            if (obj.OutputSocketCount.Value > outputSockets)
                outputSockets = Math.Min(obj.OutputSocketCount.Value, 32u);
            for (var i = 0; i != outputSockets; ++i)
            {
                addOutputSocket($"Out{i + 1}", CP77Types.Enums.questSocketType.Output);
            }
        }

        public static void AddNodeSockets_questLogicalAndNodeDefinition(CP77Types.questLogicalAndNodeDefinition obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
            => AddNodeSockets_questLogicalBaseNodeDefinition(obj, addInputSocket, addOutputSocket);

        public static void AddNodeSockets_questLogicalHubNodeDefinition(CP77Types.questLogicalHubNodeDefinition obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
            => AddNodeSockets_questLogicalBaseNodeDefinition(obj, addInputSocket, addOutputSocket);

        public static void AddNodeSockets_questLogicalXorNodeDefinition(CP77Types.questLogicalXorNodeDefinition obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
            => AddNodeSockets_questLogicalBaseNodeDefinition(obj, addInputSocket, addOutputSocket);

        public static void AddNodeSockets_questJournalNodeDefinition(CP77Types.questJournalNodeDefinition obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);

            var type = obj.Type.GetInstance();
            switch (type)
            {
                case CP77Types.questJournalBulkUpdate_NodeType _:
                case CP77Types.questJournalChangeMappinPhase_NodeType _:
                case CP77Types.questJournalTrackQuest_NodeType _:
                {
                    addInputSocket("In", CP77Types.Enums.questSocketType.Input);
                    addOutputSocket("Out", CP77Types.Enums.questSocketType.Output);
                    break;
                }
                case CP77Types.questJournalEntry_NodeType _:
                {
                    addInputSocket("Active", CP77Types.Enums.questSocketType.Input);
                    addInputSocket("Inactive", CP77Types.Enums.questSocketType.Input);
                    addOutputSocket("Out", CP77Types.Enums.questSocketType.Output);
                    break;
                }
                case CP77Types.questJournalQuestEntry_NodeType _:
                {
                    addInputSocket("Active", CP77Types.Enums.questSocketType.Input);
                    addInputSocket("Inactive", CP77Types.Enums.questSocketType.Input);
                    addInputSocket("Succeeded", CP77Types.Enums.questSocketType.Input);
                    addInputSocket("Failed", CP77Types.Enums.questSocketType.Input);
                    addOutputSocket("Out", CP77Types.Enums.questSocketType.Output);
                    break;
                }
                case CP77Types.questJournalQuestObjectiveCounter_NodeType _:
                {
                    addInputSocket("Increment", CP77Types.Enums.questSocketType.Input);
                    addInputSocket("Decrement", CP77Types.Enums.questSocketType.Input);
                    addOutputSocket("Out", CP77Types.Enums.questSocketType.Output);
                    break;
                }    
            }
        }

        public static void AddNodeSockets_questPhoneManagerNodeDefinition(CP77Types.questPhoneManagerNodeDefinition obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);

            var type = obj.Type.GetInstance();
            if (type != null)
            {
                addInputSocket("In", CP77Types.Enums.questSocketType.Input);
                addOutputSocket("Out", CP77Types.Enums.questSocketType.Output);
            }
        }

        public static void AddNodeSockets_questUseWorkspotNodeDefinition(CP77Types.questUseWorkspotNodeDefinition obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);
            addInputSocket("In", CP77Types.Enums.questSocketType.Input);
            addOutputSocket("Success", CP77Types.Enums.questSocketType.Output);

            var paramsV1 = obj.ParamsV1.GetInstance();
            if (paramsV1 != null && paramsV1.Function.Value == CP77Types.Enums.questUseWorkspotNodeFunctions.UseWorkspot)
            {
                addOutputSocket("Work Started", CP77Types.Enums.questSocketType.Output);
            }
        }

        public static void AddNodeSockets_questSwitchNodeDefinition(CP77Types.questSwitchNodeDefinition obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);
            addInputSocket("In", CP77Types.Enums.questSocketType.Input);
            foreach (var condition in obj.Conditions)
            {
                addOutputSocket($"Case{condition.SocketId.Value}", CP77Types.Enums.questSocketType.Output);

            }
            addOutputSocket("Otherwise", CP77Types.Enums.questSocketType.Output);
        }

        public static void AddNodeSockets_questPlaceholderNodeDefinition(CP77Types.questPlaceholderNodeDefinition obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            foreach (var copiedSocket in obj.CopiedSockets)
            {
                switch (copiedSocket.Type.Value)
                {
                    case CP77Types.Enums.questSocketType.Input:
                    case CP77Types.Enums.questSocketType.CutDestination:
                        addInputSocket(copiedSocket.Name.Value, copiedSocket.Type.Value);
                        break;
                    case CP77Types.Enums.questSocketType.Output:
                    case CP77Types.Enums.questSocketType.CutSource:
                        addOutputSocket(copiedSocket.Name.Value, copiedSocket.Type.Value);
                        break;
                }
            }
        }

        public static void AddNodeSockets_questRandomizerNodeDefinition(CP77Types.questRandomizerNodeDefinition obj, AddSocketFn addInputSocket, AddSocketFn addOutputSocket)
        {
            addInputSocket("CutDestination", CP77Types.Enums.questSocketType.CutDestination);
            addInputSocket("In", CP77Types.Enums.questSocketType.Input);

            var outputSockets = Math.Max(obj.OutputWeights.Count, 2);
            for (var i = 0; i != outputSockets; ++i)
            {
                addOutputSocket($"Out{i + 1}", CP77Types.Enums.questSocketType.Output);
            }
        }
    }
}
