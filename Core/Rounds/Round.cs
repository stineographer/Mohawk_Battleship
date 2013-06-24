﻿using MBC.Core.Accolades;
using MBC.Core.Events;
using MBC.Shared;
using System.Collections.Generic;

namespace MBC.Core
{
    /// <summary>
    /// Manipulates the information in provided <see cref="ControllerRegister"/>s to reflect the state of a
    /// round of battleship through events that are generated by deriving classes. <see cref="ControllerRegister"/>s
    /// should not be modified outside the scope of a deriving round class as each round depends on the state
    /// of each <see cref="ControllerRegister"/> to be modified only by the class; instead a copy should be
    /// made if modifications are desirable.
    /// </summary>
    public abstract class Round
    {
        /// <summary>
        /// The current <see cref="State"/> of game logic.
        /// </summary>
        protected internal State currentState;

        /// <summary>
        /// Registers to be manipulated.
        /// </summary>
        protected internal List<ControllerRegister> registers;

        private AccoladeGenerator accoladeGenerator;

        private List<Accolade> generatedAccolades;

        private List<Event> generatedEvents;

        private MatchInfo matchInfo;

        /// <summary>
        /// Attaches the <see cref="MatchInfo"/> from a <see cref="Match"/> and attaches the
        /// <see cref="ControllerRegister"/>s given. Initializes an empty list of <see cref="Round.Events"/>
        /// and <see cref="Round.Accolades"/>.
        /// </summary>
        /// <param name="inputRegisters">A variable number of controllers that are involved in this Round.</param>
        /// <param name="matchInfo">Information about the match that determines Round behaviour.</param>
        public Round(MatchInfo matchInfo, List<ControllerRegister> inputRegisters)
        {
            this.matchInfo = matchInfo;

            generatedEvents = new List<Event>();
            registers = new List<ControllerRegister>();
            accoladeGenerator = new AccoladeGenerator(this);

            generatedAccolades = new List<Accolade>();

            currentState = State.Begin;

            foreach (var register in inputRegisters)
            {
                registers.Add(register);
            }
        }

        /// <summary>
        /// Occurs when a controller-specific event is created.
        /// </summary>
        public event MBCControllerEventHandler ControllerEvent;

        /// <summary>
        /// Occurs when a round-specific event is created.
        /// </summary>
        public event MBCRoundEventHandler RoundEvent;

        /// <summary>
        /// Identifies various moments in the battleship game.
        /// </summary>
        public enum State
        {
            /// <summary>
            /// The beginning of the round (no events)
            /// </summary>
            Begin,

            /// <summary>
            /// The <see cref="Ship"/>s are to be placed.
            /// </summary>
            ShipPlacement,

            /// <summary>
            /// <see cref="Shot"/>s are made.
            /// </summary>
            Main,

            /// <summary>
            /// The end of the round (cessation of events).
            /// </summary>
            End
        }

        /// <summary>
        /// A list of <see cref="Accolade"/>s that have been generated based on the currently generated
        /// <see cref="Event"/>s.
        /// </summary>
        public IEnumerable<Accolade> Accolades
        {
            get
            {
                return generatedAccolades;
            }
        }

        /// <summary>
        /// Gets the current <see cref="State"/>.
        /// </summary>
        public State CurrentState
        {
            get
            {
                return currentState;
            }
        }

        /// <summary>
        /// Gets a list of <see cref="Event"/>s generated.
        /// </summary>
        public IEnumerable<Event> Events
        {
            get
            {
                return generatedEvents;
            }
        }

        /// <summary>
        /// Gets the <see cref="MatchInfo"/> associated.
        /// </summary>
        public MatchInfo MatchInfo
        {
            get
            {
                return matchInfo;
            }
        }

        /// <summary>
        /// Gets the number of <see cref="ControllerRegister"/>s involved.
        /// </summary>
        public int RegisterCount
        {
            get
            {
                return registers.Count;
            }
        }

        /// <summary>
        /// Gets a list of <see cref="ControllerRegister"/>s involved.
        /// </summary>
        public IEnumerable<ControllerRegister> Registers
        {
            get
            {
                return registers;
            }
        }

        /// <summary>
        /// Adds an <see cref="Accolade"/>. Mainly used by the internal <see cref="AccoladeGenerator"/>.
        /// </summary>
        /// <param name="accolade">The <see cref="Accolade"/> to add.</param>
        public void AddAccolade(Accolade accolade)
        {
            generatedAccolades.Add(accolade);
            MakeEvent(new RoundAccoladeEvent(this, accolade));
        }

        /// <summary>
        /// Creates a new <see cref="RoundEndEvent"/>,
        /// removes the subscriptions made to <see cref="Round.RoundEvent"/> and <see cref="Round.ControllerEvent"/>,
        /// and sets the <see cref="Round.CurrentState"/> to <see cref="State.End"/>.
        /// </summary>
        public virtual void End()
        {
            MakeEvent(new RoundEndEvent(this));
            RoundEvent = null;
            ControllerEvent = null;
            currentState = State.End;
        }

        /// <summary>
        /// Moves forward through game logic, generating <see cref="Event"/>s and updating the <see cref="ControllerRegister"/>s
        /// to reflect the <see cref="Event"/>s.
        /// </summary>
        /// <returns>A value indicating whether or not the <see cref="Round.CurrentState"/> is not
        /// equal to <see cref="State.End"/>.</returns>
        public bool Progress()
        {
            switch (currentState)
            {
                case State.Begin:
                    Begin();
                    break;

                case State.ShipPlacement:
                    ShipPlacement();
                    break;

                case State.Main:
                    Main();
                    break;
            }
            return currentState != State.End;
        }

        /// <summary>
        /// Called when the <see cref="Round.CurrentState"/> is <see cref="State.Begin"/>. Should reset the
        /// <see cref="ControllerRegister"/>s and change the <see cref="Round.CurrentState"/> to <see cref="State.ShipPlacement"/>.
        /// </summary>
        protected internal abstract void Begin();

        /// <summary>
        /// Called when the <see cref="Round.CurrentState"/> is <see cref="State.Main"/>. Should continuously
        /// generate events until the game logic has determined that the <see cref="Round.CurrentState"/> should
        /// be changed to <see cref="State.End"/>.
        /// </summary>
        protected internal abstract void Main();

        /// <summary>
        /// Creates a <see cref="ControllerEvent"/> and invokes the class's <see cref="Round.ControllerEvent"/>.
        /// </summary>
        /// <param name="ev">The <see cref="ControllerEvent"/> created.</param>
        protected void MakeEvent(ControllerEvent ev)
        {
            if (RoundEvent != null)
            {
                ControllerEvent(ev);
            }
            generatedEvents.Add(ev);
        }

        /// <summary>
        /// Creates a <see cref="RoundEvent"/> and invokes the class's <see cref="Round.RoundEvent"/>.
        /// </summary>
        /// <param name="ev">The <see cref="RoundEvent"/> created.</param>
        protected void MakeEvent(RoundEvent ev)
        {
            if (RoundEvent != null)
            {
                RoundEvent(ev);
            }
            generatedEvents.Add(ev);
        }

        /// <summary>
        /// Called when the <see cref="Round.CurrentState"/> is <see cref="State.ShipPlacement"/>. Should
        /// modify the <see cref="ControllerRegister"/>s to contain ships that have been placed.
        /// <see cref="Round.CurrentState"/> should then be changed to <see cref="State.Main"/>.
        /// </summary>
        protected abstract void ShipPlacement();
    }
}