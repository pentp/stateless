﻿using System;
using System.Collections.Generic;

namespace Stateless.Reflection
{
    /// <summary>
    /// 
    /// </summary>
    public class DynamicStateInfo
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public DynamicStateInfo(string destinationState, string criterion)
        {
            DestinationState = destinationState;
            Criterion = criterion;
        }

        /// <summary>
        /// The name of the destination state
        /// </summary>
        public string DestinationState { get; set; }

        /// <summary>
        /// The reason this destination state was chosen
        /// </summary>
        public string Criterion { get; set; }
    }

    /// <summary>
    /// List of DynamicStateInfo objects, with "add" function for ease of definition
    /// </summary>
    public class DynamicStateInfos : List<DynamicStateInfo>
    {
        /// <summary>
        /// Add a DynamicStateInfo with less typing
        /// </summary>
        /// <param name="destinationState"></param>
        /// <param name="criterion"></param>
        public void Add(string destinationState, string criterion)
        {
            base.Add(new DynamicStateInfo(destinationState, criterion));
        }

        /// <summary>
        /// Add a DynamicStateInfo with less typing
        /// </summary>
        /// <param name="destinationState"></param>
        /// <param name="criterion"></param>
        public void Add<TState>(TState destinationState, string criterion)
        {
            base.Add(new DynamicStateInfo(destinationState.ToString(), criterion));
        }
    }

    /// <summary>
    /// Describes a transition that can be initiated from a trigger, but whose result is non-deterministic.
    /// </summary>
    public class DynamicTransitionInfo : TransitionInfo
    {
        /// <summary>
        /// Gets method information for the destination state selector.
        /// </summary>
        public InvocationInfo DestinationStateSelectorDescription { get; private set; }

        /// <summary>
        /// Gets the possible destination states.
        /// </summary>
        public DynamicStateInfos PossibleDestinationStates { get; private set; }

        internal DynamicTransitionInfo(object trigger, IEnumerable<InvocationInfo> guards, InvocationInfo selector, DynamicStateInfos possibleStates)
        {
            Trigger = new TriggerInfo(trigger);
            GuardConditionsMethodDescriptions = guards ?? Array.Empty<InvocationInfo>();
            DestinationStateSelectorDescription = selector;
            PossibleDestinationStates = possibleStates;
        }
    }
}
