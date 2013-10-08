﻿using System;
using System.Collections.Generic;
using System.IO;
using MBC.Core.Controllers;
using MBC.Core.Events;
using MBC.Core.Rounds;
using MBC.Core.Util;
using MBC.Shared;
using MBC.Shared.Attributes;

namespace MBC.Core.Matches
{
    public class ActiveMatch : Match
    {
        protected Dictionary<IDNumber, IController> controllers;
        protected GameLogic currentRound;

        public ActiveMatch(Configuration conf)
        {
            Events = new EventDriver();
            Events.EventApplied += ReflectEvent;
            controllers = new Dictionary<IDNumber, IController>();

            ApplyEvent(new MatchBeginEvent(ID));
            SetConfiguration(conf);

            AddEventAction(typeof(MatchAddPlayerEvent), ControllersUpdateRegisters);
            AddEventAction(typeof(MatchRemovePlayerEvent), MatchRemovePlayer);
            AddEventAction(typeof(PlayerTeamAssignEvent), ControllersUpdateTeams);
            AddEventAction(typeof(PlayerTeamUnassignEvent), ControllersUpdateTeams);
            AddEventAction(typeof(MatchTeamCreateEvent), ControllersUpdateTeams);
        }

        public ActiveMatch()
            : this(Configuration.Global)
        {
        }

        public Configuration Config
        {
            get;
            private set;
        }

        public IDictionary<IDNumber, IController> Controllers
        {
            get
            {
                return controllers;
            }
        }

        protected EventDriver Events
        {
            get;
            private set;
        }

        public virtual void AddController(IController plr)
        {
            for (int i = 0; i < controllers.Count + 1; i++)
            {
                if (!controllers.ContainsKey(i))
                {
                    controllers[i] = plr;
                    plr.NewMatch();
                    ApplyEvent(new MatchAddPlayerEvent(i, plr.GetAttribute<NameAttribute>().ToString()));
                    return;
                }
            }
        }

        public void End()
        {
            Stop();
            ApplyEvent(new MatchEndEvent());
        }

        public virtual void Play()
        {
            IsPlaying = true;
            while (IsPlaying)
            {
            }
        }

        public void SetConfiguration(Configuration config)
        {
            Config = config;
            var newConfig = new MatchConfig();
            newConfig.FieldSize = new Coordinates(Config.GetValue<int>("mbc_field_width"), Config.GetValue<int>("mbc_field_height"));
            newConfig.NumberOfRounds = Config.GetValue<int>("mbc_match_rounds");

            newConfig.StartingShips = new ShipList();
            foreach (var length in Config.GetList<int>("mbc_ship_sizes"))
            {
                newConfig.StartingShips.Add(new Ship(length));
            }

            newConfig.TimeLimit = Config.GetValue<int>("mbc_player_thread_timeout");

            newConfig.GameMode = 0;
            foreach (var mode in Config.GetList<GameMode>("mbc_game_mode"))
            {
                newConfig.GameMode |= mode;
            }
            if (!newConfig.GameMode.HasFlag(GameMode.Classic))
            {
                throw new NotImplementedException("The " + newConfig.GameMode.ToString() + " game mode is not supported.");
            }
            ApplyEvent(new MatchConfigChangedEvent(newConfig));
        }

        public virtual void SetControllerToTeam(IDNumber ctrl, IDNumber team)
        {
            if (!controllers.ContainsKey(ctrl))
            {
                throw new ArgumentException("Controller ID number " + ctrl + " does not exist in the current match.");
            }
            if (!Teams.ContainsKey(team))
            {
                throw new ArgumentException("Team ID number " + team + " does not exist in the current match.");
            }
            ApplyEvent(new PlayerTeamAssignEvent(ctrl, team));
        }

        public virtual void Stop()
        {
            IsPlaying = false;
        }

        public virtual void UnsetControllerFromTeam(IDNumber ctrl, IDNumber team)
        {
            if (!controllers.ContainsKey(ctrl))
            {
                throw new ArgumentException("Controller ID number " + ctrl + " does not exist in the current match.");
            }
            if (!Teams.ContainsKey(team))
            {
                throw new ArgumentException("Team ID number " + team + " does not exist in the current match.");
            }
            ApplyEvent(new PlayerTeamUnassignEvent(ctrl, team));
        }

        protected internal void ApplyEvent(Event ev)
        {
            Events.ApplyEvent(ev);
        }

        private void ControllersUpdateRegisters(Event ev)
        {
            if (Events.AtEnd)
            {
                foreach (var controller in controllers)
                {
                    controller.Value.Registers = (Dictionary<IDNumber, Register>)Registers;
                }
            }
        }

        private void ControllersUpdateTeams(Event ev)
        {
            if (Events.AtEnd)
            {
                foreach (var controller in controllers)
                {
                    controller.Value.Teams = (Dictionary<IDNumber, Team>)Teams;
                }
            }
        }

        private void MatchRemovePlayer(Event ev)
        {
            var removePlayer = (MatchAddPlayerEvent)ev;
            controllers.Remove(removePlayer.PlayerID);
            ControllersUpdateRegisters(ev);
        }
    }
}