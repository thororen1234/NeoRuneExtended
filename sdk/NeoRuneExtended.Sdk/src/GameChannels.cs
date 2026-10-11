namespace NeoRune
{
    /// <summary>
    /// The game's message channels (its SW.GMR gameplay tags), for <see cref="GameEvents.Listen"/>. Where a channel says
    /// "Payload", that type was checked in game: listen with it (<see cref="GameEvents.StructOf{T}"/>) and read it with
    /// <see cref="GameEvents.Read{T}"/>. For the others, <see cref="GameEvents.LogAll"/> shows when they fire.
    /// Generated from the game's tag config (build 25754144).
    /// </summary>
    public static class GameChannels
    {
        /// <summary>SW.GMR.</summary>
        public const string All = "SW.GMR";
        /// <summary>SW.GMR.Ability.</summary>
        public const string Ability = "SW.GMR.Ability";
        /// <summary>SW.GMR.Ability.ActivationFailed. Payload: <see cref="global::UE.SWCoreGAS.FSWAbilityFailPayload"/>.</summary>
        public const string AbilityActivationFailed = "SW.GMR.Ability.ActivationFailed";
        /// <summary>SW.GMR.Ability.CooldownChanged.</summary>
        public const string AbilityCooldownChanged = "SW.GMR.Ability.CooldownChanged";
        /// <summary>SW.GMR.Ability.CooldownEnded.</summary>
        public const string AbilityCooldownEnded = "SW.GMR.Ability.CooldownEnded";
        /// <summary>SW.GMR.AccountLink.</summary>
        public const string AccountLink = "SW.GMR.AccountLink";
        /// <summary>SW.GMR.AccountLink.Completed.</summary>
        public const string AccountLinkCompleted = "SW.GMR.AccountLink.Completed";
        /// <summary>SW.GMR.AccountLink.Internal.</summary>
        public const string AccountLinkInternal = "SW.GMR.AccountLink.Internal";
        /// <summary>SW.GMR.AccountLink.Internal.CodeChanged.</summary>
        public const string AccountLinkInternalCodeChanged = "SW.GMR.AccountLink.Internal.CodeChanged";
        /// <summary>SW.GMR.AccountLink.Toggle.</summary>
        public const string AccountLinkToggle = "SW.GMR.AccountLink.Toggle";
        /// <summary>SW.GMR.AccountLink.Update.</summary>
        public const string AccountLinkUpdate = "SW.GMR.AccountLink.Update";
        /// <summary>SW.GMR.AccountUnLink.</summary>
        public const string AccountUnLink = "SW.GMR.AccountUnLink";
        /// <summary>SW.GMR.AccountUnLink.Completed.</summary>
        public const string AccountUnLinkCompleted = "SW.GMR.AccountUnLink.Completed";
        /// <summary>SW.GMR.AccountUnLink.Update.</summary>
        public const string AccountUnLinkUpdate = "SW.GMR.AccountUnLink.Update";
        /// <summary>SW.GMR.ActorTornOff. Payload: <see cref="global::UE.SWCorePayloads.FActorTornOffPayload"/>.</summary>
        public const string ActorTornOff = "SW.GMR.ActorTornOff";
        /// <summary>SW.GMR.Area.</summary>
        public const string Area = "SW.GMR.Area";
        /// <summary>SW.GMR.Area.Reset.</summary>
        public const string AreaReset = "SW.GMR.Area.Reset";
        /// <summary>SW.GMR.ArenaFight.</summary>
        public const string ArenaFight = "SW.GMR.ArenaFight";
        /// <summary>SW.GMR.ArenaFight.Event.</summary>
        public const string ArenaFightEvent = "SW.GMR.ArenaFight.Event";
        /// <summary>SW.GMR.Blocked.</summary>
        public const string Blocked = "SW.GMR.Blocked";
        /// <summary>SW.GMR.Blocked.Players.</summary>
        public const string BlockedPlayers = "SW.GMR.Blocked.Players";
        /// <summary>SW.GMR.Blocked.Players.Update.</summary>
        public const string BlockedPlayersUpdate = "SW.GMR.Blocked.Players.Update";
        /// <summary>SW.GMR.Character.</summary>
        public const string Character = "SW.GMR.Character";
        /// <summary>SW.GMR.Character.Select.</summary>
        public const string CharacterSelect = "SW.GMR.Character.Select";
        /// <summary>SW.GMR.Character.Select.Result.</summary>
        public const string CharacterSelectResult = "SW.GMR.Character.Select.Result";
        /// <summary>SW.GMR.Character.Select.Start.</summary>
        public const string CharacterSelectStart = "SW.GMR.Character.Select.Start";
        /// <summary>SW.GMR.CharacterSelectionFlow.</summary>
        public const string CharacterSelectionFlow = "SW.GMR.CharacterSelectionFlow";
        /// <summary>SW.GMR.CharacterSelectionFlow.Aborted.</summary>
        public const string CharacterSelectionFlowAborted = "SW.GMR.CharacterSelectionFlow.Aborted";
        /// <summary>SW.GMR.CharacterSelectionFlow.FailedWithError.</summary>
        public const string CharacterSelectionFlowFailedWithError = "SW.GMR.CharacterSelectionFlow.FailedWithError";
        /// <summary>SW.GMR.CharacterSelectionFlow.Succeeded.</summary>
        public const string CharacterSelectionFlowSucceeded = "SW.GMR.CharacterSelectionFlow.Succeeded";
        /// <summary>SW.GMR.CheckPrivileges.</summary>
        public const string CheckPrivileges = "SW.GMR.CheckPrivileges";
        /// <summary>SW.GMR.CheckPrivileges.Begin. Payload: <see cref="global::UE.SWCoreTypes.FEmptyMessage"/>.</summary>
        public const string CheckPrivilegesBegin = "SW.GMR.CheckPrivileges.Begin";
        /// <summary>SW.GMR.CheckPrivileges.End. Payload: <see cref="global::UE.SWCoreTypes.FEmptyMessage"/>.</summary>
        public const string CheckPrivilegesEnd = "SW.GMR.CheckPrivileges.End";
        /// <summary>SW.GMR.Client.</summary>
        public const string Client = "SW.GMR.Client";
        /// <summary>SW.GMR.Client.Apply.</summary>
        public const string ClientApply = "SW.GMR.Client.Apply";
        /// <summary>SW.GMR.Client.Apply.Loadout. Payload: <see cref="global::UE.SWCoreOnline.FClientLoadoutPayload"/>.</summary>
        public const string ClientApplyLoadout = "SW.GMR.Client.Apply.Loadout";
        /// <summary>SW.GMR.Client.Move.</summary>
        public const string ClientMove = "SW.GMR.Client.Move";
        /// <summary>SW.GMR.Client.Move.Correction.</summary>
        public const string ClientMoveCorrection = "SW.GMR.Client.Move.Correction";
        /// <summary>SW.GMR.CloneAndSelectCharacter.</summary>
        public const string CloneAndSelectCharacter = "SW.GMR.CloneAndSelectCharacter";
        /// <summary>SW.GMR.CloneAndSelectCharacter.FailedWithError.</summary>
        public const string CloneAndSelectCharacterFailedWithError = "SW.GMR.CloneAndSelectCharacter.FailedWithError";
        /// <summary>SW.GMR.CloneAndSelectCharacter.Succeeded.</summary>
        public const string CloneAndSelectCharacterSucceeded = "SW.GMR.CloneAndSelectCharacter.Succeeded";
        /// <summary>SW.GMR.CreateAndSelectCharacter.</summary>
        public const string CreateAndSelectCharacter = "SW.GMR.CreateAndSelectCharacter";
        /// <summary>SW.GMR.CreateAndSelectCharacter.FailedWithError.</summary>
        public const string CreateAndSelectCharacterFailedWithError = "SW.GMR.CreateAndSelectCharacter.FailedWithError";
        /// <summary>SW.GMR.CreateAndSelectCharacter.Succeeded.</summary>
        public const string CreateAndSelectCharacterSucceeded = "SW.GMR.CreateAndSelectCharacter.Succeeded";
        /// <summary>SW.GMR.Disconnect.</summary>
        public const string Disconnect = "SW.GMR.Disconnect";
        /// <summary>SW.GMR.Disconnect.Timer.</summary>
        public const string DisconnectTimer = "SW.GMR.Disconnect.Timer";
        /// <summary>SW.GMR.Disconnect.Timer.Start.</summary>
        public const string DisconnectTimerStart = "SW.GMR.Disconnect.Timer.Start";
        /// <summary>SW.GMR.Disconnect.Timer.Stop.</summary>
        public const string DisconnectTimerStop = "SW.GMR.Disconnect.Timer.Stop";
        /// <summary>SW.GMR.DoorMapMarker.</summary>
        public const string DoorMapMarker = "SW.GMR.DoorMapMarker";
        /// <summary>SW.GMR.DoorMapMarker.RequestAdd.</summary>
        public const string DoorMapMarkerRequestAdd = "SW.GMR.DoorMapMarker.RequestAdd";
        /// <summary>SW.GMR.DoorMapMarker.RequestRemove.</summary>
        public const string DoorMapMarkerRequestRemove = "SW.GMR.DoorMapMarker.RequestRemove";
        /// <summary>SW.GMR.Force.</summary>
        public const string Force = "SW.GMR.Force";
        /// <summary>SW.GMR.Force.SpawnVolume.</summary>
        public const string ForceSpawnVolume = "SW.GMR.Force.SpawnVolume";
        /// <summary>SW.GMR.Force.SpawnVolume.Cooldown.</summary>
        public const string ForceSpawnVolumeCooldown = "SW.GMR.Force.SpawnVolume.Cooldown";
        /// <summary>SW.GMR.Friend.</summary>
        public const string Friend = "SW.GMR.Friend";
        /// <summary>SW.GMR.Friend.Invite.</summary>
        public const string FriendInvite = "SW.GMR.Friend.Invite";
        /// <summary>SW.GMR.Friend.Invite.Sent.</summary>
        public const string FriendInviteSent = "SW.GMR.Friend.Invite.Sent";
        /// <summary>SW.GMR.Friend.IsJoinable.</summary>
        public const string FriendIsJoinable = "SW.GMR.Friend.IsJoinable";
        /// <summary>SW.GMR.Friends.</summary>
        public const string Friends = "SW.GMR.Friends";
        /// <summary>SW.GMR.Friends.Update.</summary>
        public const string FriendsUpdate = "SW.GMR.Friends.Update";
        /// <summary>SW.GMR.Game.</summary>
        public const string Game = "SW.GMR.Game";
        /// <summary>SW.GMR.Game.Context.</summary>
        public const string GameContext = "SW.GMR.Game.Context";
        /// <summary>SW.GMR.Game.Context.Change.</summary>
        public const string GameContextChange = "SW.GMR.Game.Context.Change";
        /// <summary>SW.GMR.Game.Match.</summary>
        public const string GameMatch = "SW.GMR.Game.Match";
        /// <summary>SW.GMR.Game.Match.ServerRequested.</summary>
        public const string GameMatchServerRequested = "SW.GMR.Game.Match.ServerRequested";
        /// <summary>SW.GMR.Game.Match.Start.</summary>
        public const string GameMatchStart = "SW.GMR.Game.Match.Start";
        /// <summary>SW.GMR.GudianceLine.</summary>
        public const string GudianceLine = "SW.GMR.GudianceLine";
        /// <summary>SW.GMR.GudianceLine.Trigger. Payload: <see cref="global::UE.SWCoreTypes.FEmptyMessage"/>.</summary>
        public const string GudianceLineTrigger = "SW.GMR.GudianceLine.Trigger";
        /// <summary>SW.GMR.Inventory.</summary>
        public const string Inventory = "SW.GMR.Inventory";
        /// <summary>SW.GMR.Inventory.AssetsLoaded.</summary>
        public const string InventoryAssetsLoaded = "SW.GMR.Inventory.AssetsLoaded";
        /// <summary>SW.GMR.Inventory.Changed.</summary>
        public const string InventoryChanged = "SW.GMR.Inventory.Changed";
        /// <summary>SW.GMR.Item.</summary>
        public const string Item = "SW.GMR.Item";
        /// <summary>SW.GMR.Item.Equipped. Payload: <see cref="global::UE.InventorySystem.FPlayerItemEntryPayload"/>.</summary>
        public const string ItemEquipped = "SW.GMR.Item.Equipped";
        /// <summary>SW.GMR.Item.Highlight.</summary>
        public const string ItemHighlight = "SW.GMR.Item.Highlight";
        /// <summary>SW.GMR.LiveEventQuests.</summary>
        public const string LiveEventQuests = "SW.GMR.LiveEventQuests";
        /// <summary>SW.GMR.LiveEventQuests.Update.</summary>
        public const string LiveEventQuestsUpdate = "SW.GMR.LiveEventQuests.Update";
        /// <summary>SW.GMR.LobbyTakeover.</summary>
        public const string LobbyTakeover = "SW.GMR.LobbyTakeover";
        /// <summary>SW.GMR.LobbyTakeover.Fetched. Payload: <see cref="global::UE.LobbyMessageSubsystem.FLobbyTakeoverFetchedPayload"/>.</summary>
        public const string LobbyTakeoverFetched = "SW.GMR.LobbyTakeover.Fetched";
        /// <summary>SW.GMR.Login.</summary>
        public const string Login = "SW.GMR.Login";
        /// <summary>SW.GMR.Login.Browser.</summary>
        public const string LoginBrowser = "SW.GMR.Login.Browser";
        /// <summary>SW.GMR.Login.Browser.Close.</summary>
        public const string LoginBrowserClose = "SW.GMR.Login.Browser.Close";
        /// <summary>SW.GMR.Login.Browser.URL.</summary>
        public const string LoginBrowserURL = "SW.GMR.Login.Browser.URL";
        /// <summary>SW.GMR.Login.Browser.URL.Changed.</summary>
        public const string LoginBrowserURLChanged = "SW.GMR.Login.Browser.URL.Changed";
        /// <summary>SW.GMR.Matchmake.</summary>
        public const string Matchmake = "SW.GMR.Matchmake";
        /// <summary>SW.GMR.Matchmake.Cancel.</summary>
        public const string MatchmakeCancel = "SW.GMR.Matchmake.Cancel";
        /// <summary>SW.GMR.Matchmake.Complete.</summary>
        public const string MatchmakeComplete = "SW.GMR.Matchmake.Complete";
        /// <summary>SW.GMR.Matchmake.Start.</summary>
        public const string MatchmakeStart = "SW.GMR.Matchmake.Start";
        /// <summary>SW.GMR.MinecartStation.</summary>
        public const string MinecartStation = "SW.GMR.MinecartStation";
        /// <summary>SW.GMR.MinecartStation.Discover.</summary>
        public const string MinecartStationDiscover = "SW.GMR.MinecartStation.Discover";
        /// <summary>SW.GMR.MinecartStation.Interact.</summary>
        public const string MinecartStationInteract = "SW.GMR.MinecartStation.Interact";
        /// <summary>SW.GMR.MinecartStation.Interact.Begin.</summary>
        public const string MinecartStationInteractBegin = "SW.GMR.MinecartStation.Interact.Begin";
        /// <summary>SW.GMR.MinecartStation.Interact.End.</summary>
        public const string MinecartStationInteractEnd = "SW.GMR.MinecartStation.Interact.End";
        /// <summary>SW.GMR.NMT.</summary>
        public const string NMT = "SW.GMR.NMT";
        /// <summary>SW.GMR.NMT.SecondaryPlayerRemoved.</summary>
        public const string NMTSecondaryPlayerRemoved = "SW.GMR.NMT.SecondaryPlayerRemoved";
        /// <summary>SW.GMR.NMT.SecondaryPlayerRemoved.Receive.</summary>
        public const string NMTSecondaryPlayerRemovedReceive = "SW.GMR.NMT.SecondaryPlayerRemoved.Receive";
        /// <summary>SW.GMR.Onboarding.</summary>
        public const string Onboarding = "SW.GMR.Onboarding";
        /// <summary>SW.GMR.Onboarding.Complete. Payload: <see cref="global::UE.SWCorePayloads.FOnboardingPayload"/>.</summary>
        public const string OnboardingComplete = "SW.GMR.Onboarding.Complete";
        /// <summary>SW.GMR.Onboarding.Complete.ActionList.</summary>
        public const string OnboardingCompleteActionList = "SW.GMR.Onboarding.Complete.ActionList";
        /// <summary>SW.GMR.Onboarding.Complete.Banner.</summary>
        public const string OnboardingCompleteBanner = "SW.GMR.Onboarding.Complete.Banner";
        /// <summary>SW.GMR.Onboarding.Complete.Custom.</summary>
        public const string OnboardingCompleteCustom = "SW.GMR.Onboarding.Complete.Custom";
        /// <summary>SW.GMR.Onboarding.Complete.InWorld.</summary>
        public const string OnboardingCompleteInWorld = "SW.GMR.Onboarding.Complete.InWorld";
        /// <summary>SW.GMR.Onboarding.Complete.Panel.</summary>
        public const string OnboardingCompletePanel = "SW.GMR.Onboarding.Complete.Panel";
        /// <summary>SW.GMR.Onboarding.Complete.TextPanel.</summary>
        public const string OnboardingCompleteTextPanel = "SW.GMR.Onboarding.Complete.TextPanel";
        /// <summary>SW.GMR.Onboarding.Complete.Toast.</summary>
        public const string OnboardingCompleteToast = "SW.GMR.Onboarding.Complete.Toast";
        /// <summary>SW.GMR.Onboarding.Complete.TutorialEntry.</summary>
        public const string OnboardingCompleteTutorialEntry = "SW.GMR.Onboarding.Complete.TutorialEntry";
        /// <summary>SW.GMR.Onboarding.Dismiss.</summary>
        public const string OnboardingDismiss = "SW.GMR.Onboarding.Dismiss";
        /// <summary>SW.GMR.Onboarding.FeelingLonely.</summary>
        public const string OnboardingFeelingLonely = "SW.GMR.Onboarding.FeelingLonely";
        /// <summary>SW.GMR.Onboarding.Start. Payload: <see cref="global::UE.SWCorePayloads.FOnboardingPayload"/>.</summary>
        public const string OnboardingStart = "SW.GMR.Onboarding.Start";
        /// <summary>SW.GMR.Onboarding.Start.ActionList.</summary>
        public const string OnboardingStartActionList = "SW.GMR.Onboarding.Start.ActionList";
        /// <summary>SW.GMR.Onboarding.Start.Banner.</summary>
        public const string OnboardingStartBanner = "SW.GMR.Onboarding.Start.Banner";
        /// <summary>SW.GMR.Onboarding.Start.Custom.</summary>
        public const string OnboardingStartCustom = "SW.GMR.Onboarding.Start.Custom";
        /// <summary>SW.GMR.Onboarding.Start.InWorld.</summary>
        public const string OnboardingStartInWorld = "SW.GMR.Onboarding.Start.InWorld";
        /// <summary>SW.GMR.Onboarding.Start.Panel.</summary>
        public const string OnboardingStartPanel = "SW.GMR.Onboarding.Start.Panel";
        /// <summary>SW.GMR.Onboarding.Start.TextPanel.</summary>
        public const string OnboardingStartTextPanel = "SW.GMR.Onboarding.Start.TextPanel";
        /// <summary>SW.GMR.Onboarding.Start.Toast.</summary>
        public const string OnboardingStartToast = "SW.GMR.Onboarding.Start.Toast";
        /// <summary>SW.GMR.Onboarding.Start.TutorialEntry.</summary>
        public const string OnboardingStartTutorialEntry = "SW.GMR.Onboarding.Start.TutorialEntry";
        /// <summary>SW.GMR.Party.</summary>
        public const string Party = "SW.GMR.Party";
        /// <summary>SW.GMR.Party.Join.</summary>
        public const string PartyJoin = "SW.GMR.Party.Join";
        /// <summary>SW.GMR.Party.Join.Attempted.</summary>
        public const string PartyJoinAttempted = "SW.GMR.Party.Join.Attempted";
        /// <summary>SW.GMR.Party.Matchmaking.</summary>
        public const string PartyMatchmaking = "SW.GMR.Party.Matchmaking";
        /// <summary>SW.GMR.Party.Matchmaking.Request.</summary>
        public const string PartyMatchmakingRequest = "SW.GMR.Party.Matchmaking.Request";
        /// <summary>SW.GMR.Party.Matchmaking.Request.Taking.</summary>
        public const string PartyMatchmakingRequestTaking = "SW.GMR.Party.Matchmaking.Request.Taking";
        /// <summary>SW.GMR.Party.Matchmaking.Request.Taking.Long.</summary>
        public const string PartyMatchmakingRequestTakingLong = "SW.GMR.Party.Matchmaking.Request.Taking.Long";
        /// <summary>SW.GMR.Party.Server.</summary>
        public const string PartyServer = "SW.GMR.Party.Server";
        /// <summary>SW.GMR.Party.Server.Request.</summary>
        public const string PartyServerRequest = "SW.GMR.Party.Server.Request";
        /// <summary>SW.GMR.Party.Server.Request.Taking.</summary>
        public const string PartyServerRequestTaking = "SW.GMR.Party.Server.Request.Taking";
        /// <summary>SW.GMR.Party.Server.Request.Taking.Long.</summary>
        public const string PartyServerRequestTakingLong = "SW.GMR.Party.Server.Request.Taking.Long";
        /// <summary>SW.GMR.Party.Sign.</summary>
        public const string PartySign = "SW.GMR.Party.Sign";
        /// <summary>SW.GMR.Party.Sign.Out.</summary>
        public const string PartySignOut = "SW.GMR.Party.Sign.Out";
        /// <summary>SW.GMR.Party.Sign.Out.Player.</summary>
        public const string PartySignOutPlayer = "SW.GMR.Party.Sign.Out.Player";
        /// <summary>SW.GMR.Party.Update.</summary>
        public const string PartyUpdate = "SW.GMR.Party.Update";
        /// <summary>SW.GMR.PartyVoting.</summary>
        public const string PartyVoting = "SW.GMR.PartyVoting";
        /// <summary>SW.GMR.PartyVoting.Cancelled.</summary>
        public const string PartyVotingCancelled = "SW.GMR.PartyVoting.Cancelled";
        /// <summary>SW.GMR.PartyVoting.Complete.</summary>
        public const string PartyVotingComplete = "SW.GMR.PartyVoting.Complete";
        /// <summary>SW.GMR.PartyVoting.Ended.</summary>
        public const string PartyVotingEnded = "SW.GMR.PartyVoting.Ended";
        /// <summary>SW.GMR.PartyVoting.Start.</summary>
        public const string PartyVotingStart = "SW.GMR.PartyVoting.Start";
        /// <summary>SW.GMR.PartyVoting.VotesUpdated.</summary>
        public const string PartyVotingVotesUpdated = "SW.GMR.PartyVoting.VotesUpdated";
        /// <summary>SW.GMR.Pawn.</summary>
        public const string Pawn = "SW.GMR.Pawn";
        /// <summary>SW.GMR.Pawn.Local.</summary>
        public const string PawnLocal = "SW.GMR.Pawn.Local";
        /// <summary>SW.GMR.Pawn.Local.Possessed.</summary>
        public const string PawnLocalPossessed = "SW.GMR.Pawn.Local.Possessed";
        /// <summary>SW.GMR.Pawn.Local.Unpossessed.</summary>
        public const string PawnLocalUnpossessed = "SW.GMR.Pawn.Local.Unpossessed";
        /// <summary>SW.GMR.Pawn.Online.</summary>
        public const string PawnOnline = "SW.GMR.Pawn.Online";
        /// <summary>SW.GMR.Pawn.Online.Possessed.</summary>
        public const string PawnOnlinePossessed = "SW.GMR.Pawn.Online.Possessed";
        /// <summary>SW.GMR.Pawn.Online.Unpossessed.</summary>
        public const string PawnOnlineUnpossessed = "SW.GMR.Pawn.Online.Unpossessed";
        /// <summary>SW.GMR.Platform.</summary>
        public const string Platform = "SW.GMR.Platform";
        /// <summary>SW.GMR.Platform.FTUE.</summary>
        public const string PlatformFTUE = "SW.GMR.Platform.FTUE";
        /// <summary>SW.GMR.Platform.FTUE.Crossplay.</summary>
        public const string PlatformFTUECrossplay = "SW.GMR.Platform.FTUE.Crossplay";
        /// <summary>SW.GMR.Platform.FTUE.Crossplay.Enabled.</summary>
        public const string PlatformFTUECrossplayEnabled = "SW.GMR.Platform.FTUE.Crossplay.Enabled";
        /// <summary>SW.GMR.Platform.Net.</summary>
        public const string PlatformNet = "SW.GMR.Platform.Net";
        /// <summary>SW.GMR.Platform.Net.Connect.</summary>
        public const string PlatformNetConnect = "SW.GMR.Platform.Net.Connect";
        /// <summary>SW.GMR.Platform.Net.Disconnect.</summary>
        public const string PlatformNetDisconnect = "SW.GMR.Platform.Net.Disconnect";
        /// <summary>SW.GMR.Platform.Privileges.</summary>
        public const string PlatformPrivileges = "SW.GMR.Platform.Privileges";
        /// <summary>SW.GMR.Platform.Privileges.Updated.</summary>
        public const string PlatformPrivilegesUpdated = "SW.GMR.Platform.Privileges.Updated";
        /// <summary>SW.GMR.Player.</summary>
        public const string Player = "SW.GMR.Player";
        /// <summary>SW.GMR.Player.Character.</summary>
        public const string PlayerCharacter = "SW.GMR.Player.Character";
        /// <summary>SW.GMR.Player.Character.Active.</summary>
        public const string PlayerCharacterActive = "SW.GMR.Player.Character.Active";
        /// <summary>SW.GMR.Player.Character.Update.</summary>
        public const string PlayerCharacterUpdate = "SW.GMR.Player.Character.Update";
        /// <summary>SW.GMR.Player.Damage.</summary>
        public const string PlayerDamage = "SW.GMR.Player.Damage";
        /// <summary>SW.GMR.Player.Damage.Number.</summary>
        public const string PlayerDamageNumber = "SW.GMR.Player.Damage.Number";
        /// <summary>SW.GMR.Player.Inactivity.</summary>
        public const string PlayerInactivity = "SW.GMR.Player.Inactivity";
        /// <summary>SW.GMR.Player.Inactivity.Countdown.</summary>
        public const string PlayerInactivityCountdown = "SW.GMR.Player.Inactivity.Countdown";
        /// <summary>SW.GMR.Player.Inactivity.Countdown.Start.</summary>
        public const string PlayerInactivityCountdownStart = "SW.GMR.Player.Inactivity.Countdown.Start";
        /// <summary>SW.GMR.Player.Inactivity.Countdown.Stop.</summary>
        public const string PlayerInactivityCountdownStop = "SW.GMR.Player.Inactivity.Countdown.Stop";
        /// <summary>SW.GMR.PlayerController.</summary>
        public const string PlayerController = "SW.GMR.PlayerController";
        /// <summary>SW.GMR.PlayerController.Login.</summary>
        public const string PlayerControllerLogin = "SW.GMR.PlayerController.Login";
        /// <summary>SW.GMR.PlayerController.Logout.</summary>
        public const string PlayerControllerLogout = "SW.GMR.PlayerController.Logout";
        /// <summary>SW.GMR.PlayerController.PreLogout.</summary>
        public const string PlayerControllerPreLogout = "SW.GMR.PlayerController.PreLogout";
        /// <summary>SW.GMR.PlayerTeleportCompleted.</summary>
        public const string PlayerTeleportCompleted = "SW.GMR.PlayerTeleportCompleted";
        /// <summary>SW.GMR.PlayerTeleportCompletedTelemetry. Payload: <see cref="global::UE.SpicewoodGAS.FTeleportCompleteTelemetryMessage"/>.</summary>
        public const string PlayerTeleportCompletedTelemetry = "SW.GMR.PlayerTeleportCompletedTelemetry";
        /// <summary>SW.GMR.PlayerTeleportStarted. Payload: <see cref="global::UE.SWCoreTypes.FEmptyMessage"/>.</summary>
        public const string PlayerTeleportStarted = "SW.GMR.PlayerTeleportStarted";
        /// <summary>SW.GMR.PrivilegeCheck.</summary>
        public const string PrivilegeCheck = "SW.GMR.PrivilegeCheck";
        /// <summary>SW.GMR.PrivilegeCheck.Complete.</summary>
        public const string PrivilegeCheckComplete = "SW.GMR.PrivilegeCheck.Complete";
        /// <summary>SW.GMR.Quest.</summary>
        public const string Quest = "SW.GMR.Quest";
        /// <summary>SW.GMR.Quest.Acceptance.</summary>
        public const string QuestAcceptance = "SW.GMR.Quest.Acceptance";
        /// <summary>SW.GMR.Quest.Acceptance.ShowUpdate.</summary>
        public const string QuestAcceptanceShowUpdate = "SW.GMR.Quest.Acceptance.ShowUpdate";
        /// <summary>SW.GMR.RedeemCode.</summary>
        public const string RedeemCode = "SW.GMR.RedeemCode";
        /// <summary>SW.GMR.RedeemCode.Failed.</summary>
        public const string RedeemCodeFailed = "SW.GMR.RedeemCode.Failed";
        /// <summary>SW.GMR.RedeemCode.Succeeded.</summary>
        public const string RedeemCodeSucceeded = "SW.GMR.RedeemCode.Succeeded";
        /// <summary>SW.GMR.SaveGame.</summary>
        public const string SaveGame = "SW.GMR.SaveGame";
        /// <summary>SW.GMR.SaveGame.Begin.</summary>
        public const string SaveGameBegin = "SW.GMR.SaveGame.Begin";
        /// <summary>SW.GMR.SaveGame.Error.</summary>
        public const string SaveGameError = "SW.GMR.SaveGame.Error";
        /// <summary>SW.GMR.Secondary.</summary>
        public const string Secondary = "SW.GMR.Secondary";
        /// <summary>SW.GMR.Secondary.Player.</summary>
        public const string SecondaryPlayer = "SW.GMR.Secondary.Player";
        /// <summary>SW.GMR.Secondary.Player.Input.</summary>
        public const string SecondaryPlayerInput = "SW.GMR.Secondary.Player.Input";
        /// <summary>SW.GMR.Secondary.Player.Input.Connected.</summary>
        public const string SecondaryPlayerInputConnected = "SW.GMR.Secondary.Player.Input.Connected";
        /// <summary>SW.GMR.Secondary.Player.Input.Connected.Changed.</summary>
        public const string SecondaryPlayerInputConnectedChanged = "SW.GMR.Secondary.Player.Input.Connected.Changed";
        /// <summary>SW.GMR.Secondary.Player.Removed.</summary>
        public const string SecondaryPlayerRemoved = "SW.GMR.Secondary.Player.Removed";
        /// <summary>SW.GMR.Secondary.Player.Removed.Inactivity.</summary>
        public const string SecondaryPlayerRemovedInactivity = "SW.GMR.Secondary.Player.Removed.Inactivity";
        /// <summary>SW.GMR.Secondary.Player.Request.</summary>
        public const string SecondaryPlayerRequest = "SW.GMR.Secondary.Player.Request";
        /// <summary>SW.GMR.Secondary.Player.Request.Leave.</summary>
        public const string SecondaryPlayerRequestLeave = "SW.GMR.Secondary.Player.Request.Leave";
        /// <summary>SW.GMR.SelectActiveCharacter.</summary>
        public const string SelectActiveCharacter = "SW.GMR.SelectActiveCharacter";
        /// <summary>SW.GMR.SelectActiveCharacter.FailedWithError.</summary>
        public const string SelectActiveCharacterFailedWithError = "SW.GMR.SelectActiveCharacter.FailedWithError";
        /// <summary>SW.GMR.SelectActiveCharacter.Succeeded.</summary>
        public const string SelectActiveCharacterSucceeded = "SW.GMR.SelectActiveCharacter.Succeeded";
        /// <summary>SW.GMR.Server.</summary>
        public const string Server = "SW.GMR.Server";
        /// <summary>SW.GMR.Server.Apply.</summary>
        public const string ServerApply = "SW.GMR.Server.Apply";
        /// <summary>SW.GMR.Server.Apply.Loadout.</summary>
        public const string ServerApplyLoadout = "SW.GMR.Server.Apply.Loadout";
        /// <summary>SW.GMR.Server.Load.</summary>
        public const string ServerLoad = "SW.GMR.Server.Load";
        /// <summary>SW.GMR.Server.Load.Character.</summary>
        public const string ServerLoadCharacter = "SW.GMR.Server.Load.Character";
        /// <summary>SW.GMR.Server.Load.Character.Error.</summary>
        public const string ServerLoadCharacterError = "SW.GMR.Server.Load.Character.Error";
        /// <summary>SW.GMR.Session.</summary>
        public const string Session = "SW.GMR.Session";
        /// <summary>SW.GMR.Session.Invites.</summary>
        public const string SessionInvites = "SW.GMR.Session.Invites";
        /// <summary>SW.GMR.Session.Invites.Update.</summary>
        public const string SessionInvitesUpdate = "SW.GMR.Session.Invites.Update";
        /// <summary>SW.GMR.Set.</summary>
        public const string Set = "SW.GMR.Set";
        /// <summary>SW.GMR.Set.Online.</summary>
        public const string SetOnline = "SW.GMR.Set.Online";
        /// <summary>SW.GMR.Set.Online.DataTable.</summary>
        public const string SetOnlineDataTable = "SW.GMR.Set.Online.DataTable";
        /// <summary>SW.GMR.Set.Online.DataTable.Text.</summary>
        public const string SetOnlineDataTableText = "SW.GMR.Set.Online.DataTable.Text";
        /// <summary>SW.GMR.Settings.</summary>
        public const string Settings = "SW.GMR.Settings";
        /// <summary>SW.GMR.Settings.Applied. Payload: <see cref="global::UE.SWCorePayloads.FSettingChangedPayload"/>.</summary>
        public const string SettingsApplied = "SW.GMR.Settings.Applied";
        /// <summary>SW.GMR.Settings.Applied.ColorToken. Payload: <see cref="global::UE.UIColorTokens.FColorTokenMessage"/>.</summary>
        public const string SettingsAppliedColorToken = "SW.GMR.Settings.Applied.ColorToken";
        /// <summary>SW.GMR.Settings.Applied.DualsenseAdaptiveTrigger.</summary>
        public const string SettingsAppliedDualsenseAdaptiveTrigger = "SW.GMR.Settings.Applied.DualsenseAdaptiveTrigger";
        /// <summary>SW.GMR.Settings.Applied.EventLog.</summary>
        public const string SettingsAppliedEventLog = "SW.GMR.Settings.Applied.EventLog";
        /// <summary>SW.GMR.Settings.Applied.EventLog.MessageSound.</summary>
        public const string SettingsAppliedEventLogMessageSound = "SW.GMR.Settings.Applied.EventLog.MessageSound";
        /// <summary>SW.GMR.Settings.Applied.GamepadVibration.</summary>
        public const string SettingsAppliedGamepadVibration = "SW.GMR.Settings.Applied.GamepadVibration";
        /// <summary>SW.GMR.Settings.Applied.Gfx.</summary>
        public const string SettingsAppliedGfx = "SW.GMR.Settings.Applied.Gfx";
        /// <summary>SW.GMR.Settings.Applied.Gfx.PostProssesQuality.</summary>
        public const string SettingsAppliedGfxPostProssesQuality = "SW.GMR.Settings.Applied.Gfx.PostProssesQuality";
        /// <summary>SW.GMR.Settings.Applied.Gfx.ShadowQuality.</summary>
        public const string SettingsAppliedGfxShadowQuality = "SW.GMR.Settings.Applied.Gfx.ShadowQuality";
        /// <summary>SW.GMR.Settings.Applied.Gfx.TextureQuality.</summary>
        public const string SettingsAppliedGfxTextureQuality = "SW.GMR.Settings.Applied.Gfx.TextureQuality";
        /// <summary>SW.GMR.Settings.Applied.Gfx.VisualEffectQuality.</summary>
        public const string SettingsAppliedGfxVisualEffectQuality = "SW.GMR.Settings.Applied.Gfx.VisualEffectQuality";
        /// <summary>SW.GMR.Settings.Applied.High.</summary>
        public const string SettingsAppliedHigh = "SW.GMR.Settings.Applied.High";
        /// <summary>SW.GMR.Settings.Applied.High.Contrast.</summary>
        public const string SettingsAppliedHighContrast = "SW.GMR.Settings.Applied.High.Contrast";
        /// <summary>SW.GMR.Settings.Applied.High.Contrast.Mode. Payload: <see cref="global::UE.SWCoreTypes.FHighContrastMessage"/>.</summary>
        public const string SettingsAppliedHighContrastMode = "SW.GMR.Settings.Applied.High.Contrast.Mode";
        /// <summary>SW.GMR.Settings.Applied.Inventory.</summary>
        public const string SettingsAppliedInventory = "SW.GMR.Settings.Applied.Inventory";
        /// <summary>SW.GMR.Settings.Applied.Inventory.MiniInventoryAsPrimary.</summary>
        public const string SettingsAppliedInventoryMiniInventoryAsPrimary = "SW.GMR.Settings.Applied.Inventory.MiniInventoryAsPrimary";
        /// <summary>SW.GMR.Settings.Applied.Invincibility.</summary>
        public const string SettingsAppliedInvincibility = "SW.GMR.Settings.Applied.Invincibility";
        /// <summary>SW.GMR.Settings.Applied.ReadableFont.</summary>
        public const string SettingsAppliedReadableFont = "SW.GMR.Settings.Applied.ReadableFont";
        /// <summary>SW.GMR.Settings.Applied.ReduceUIMotion.</summary>
        public const string SettingsAppliedReduceUIMotion = "SW.GMR.Settings.Applied.ReduceUIMotion";
        /// <summary>SW.GMR.Settings.Applied.Show.</summary>
        public const string SettingsAppliedShow = "SW.GMR.Settings.Applied.Show";
        /// <summary>SW.GMR.Settings.Applied.Show.Souls.</summary>
        public const string SettingsAppliedShowSouls = "SW.GMR.Settings.Applied.Show.Souls";
        /// <summary>SW.GMR.Settings.Applied.Show.Souls.Cost.</summary>
        public const string SettingsAppliedShowSoulsCost = "SW.GMR.Settings.Applied.Show.Souls.Cost";
        /// <summary>SW.GMR.Settings.Applied.Subtitles.</summary>
        public const string SettingsAppliedSubtitles = "SW.GMR.Settings.Applied.Subtitles";
        /// <summary>SW.GMR.Settings.Applied.Subtitles.BackgroundOpacity.</summary>
        public const string SettingsAppliedSubtitlesBackgroundOpacity = "SW.GMR.Settings.Applied.Subtitles.BackgroundOpacity";
        /// <summary>SW.GMR.Settings.Applied.Subtitles.Color.</summary>
        public const string SettingsAppliedSubtitlesColor = "SW.GMR.Settings.Applied.Subtitles.Color";
        /// <summary>SW.GMR.Settings.Applied.Subtitles.Size.</summary>
        public const string SettingsAppliedSubtitlesSize = "SW.GMR.Settings.Applied.Subtitles.Size";
        /// <summary>SW.GMR.Settings.DifficultyLevel.</summary>
        public const string SettingsDifficultyLevel = "SW.GMR.Settings.DifficultyLevel";
        /// <summary>SW.GMR.Settings.DifficultyLevel.Changed.</summary>
        public const string SettingsDifficultyLevelChanged = "SW.GMR.Settings.DifficultyLevel.Changed";
        /// <summary>SW.GMR.Settings.Loaded. Payload: <see cref="global::UE.SWCorePayloads.FSettingsLoadedPayload"/>.</summary>
        public const string SettingsLoaded = "SW.GMR.Settings.Loaded";
        /// <summary>SW.GMR.Settings.Onboarding.</summary>
        public const string SettingsOnboarding = "SW.GMR.Settings.Onboarding";
        /// <summary>SW.GMR.Settings.Onboarding.Enabled.</summary>
        public const string SettingsOnboardingEnabled = "SW.GMR.Settings.Onboarding.Enabled";
        /// <summary>SW.GMR.ShowCredits.</summary>
        public const string ShowCredits = "SW.GMR.ShowCredits";
        /// <summary>SW.GMR.SoulImbalance.</summary>
        public const string SoulImbalance = "SW.GMR.SoulImbalance";
        /// <summary>SW.GMR.SoulImbalance.AppliedToPlayer.</summary>
        public const string SoulImbalanceAppliedToPlayer = "SW.GMR.SoulImbalance.AppliedToPlayer";
        /// <summary>SW.GMR.SoulImbalance.RemovedFromPlayer.</summary>
        public const string SoulImbalanceRemovedFromPlayer = "SW.GMR.SoulImbalance.RemovedFromPlayer";
        /// <summary>SW.GMR.SoulStatedMob.</summary>
        public const string SoulStatedMob = "SW.GMR.SoulStatedMob";
        /// <summary>SW.GMR.SoulStatedMob.Added.</summary>
        public const string SoulStatedMobAdded = "SW.GMR.SoulStatedMob.Added";
        /// <summary>SW.GMR.SoulStatedMob.Despawn.</summary>
        public const string SoulStatedMobDespawn = "SW.GMR.SoulStatedMob.Despawn";
        /// <summary>SW.GMR.SoulStatedMob.Highlight.</summary>
        public const string SoulStatedMobHighlight = "SW.GMR.SoulStatedMob.Highlight";
        /// <summary>SW.GMR.SoulStatedMob.Removed.</summary>
        public const string SoulStatedMobRemoved = "SW.GMR.SoulStatedMob.Removed";
        /// <summary>SW.GMR.SoulStatedMob.Spawn.</summary>
        public const string SoulStatedMobSpawn = "SW.GMR.SoulStatedMob.Spawn";
        /// <summary>SW.GMR.SoulStorm.</summary>
        public const string SoulStorm = "SW.GMR.SoulStorm";
        /// <summary>SW.GMR.SoulStorm.DamageThreshold.</summary>
        public const string SoulStormDamageThreshold = "SW.GMR.SoulStorm.DamageThreshold";
        /// <summary>SW.GMR.SoulStorm.End.</summary>
        public const string SoulStormEnd = "SW.GMR.SoulStorm.End";
        /// <summary>SW.GMR.SoulStorm.Enter.</summary>
        public const string SoulStormEnter = "SW.GMR.SoulStorm.Enter";
        /// <summary>SW.GMR.SoulStorm.Exit.</summary>
        public const string SoulStormExit = "SW.GMR.SoulStorm.Exit";
        /// <summary>SW.GMR.SoulStorm.NewWave.</summary>
        public const string SoulStormNewWave = "SW.GMR.SoulStorm.NewWave";
        /// <summary>SW.GMR.SoulStorm.Spawn.</summary>
        public const string SoulStormSpawn = "SW.GMR.SoulStorm.Spawn";
        /// <summary>SW.GMR.SoulStorm.Update.</summary>
        public const string SoulStormUpdate = "SW.GMR.SoulStorm.Update";
        /// <summary>SW.GMR.TimePhase.</summary>
        public const string TimePhase = "SW.GMR.TimePhase";
        /// <summary>SW.GMR.TimePhase.Changed. Payload: <see cref="global::UE.SWCorePayloads.FTimePhasePayload"/>.</summary>
        public const string TimePhaseChanged = "SW.GMR.TimePhase.Changed";
        /// <summary>SW.GMR.TutorialLog.</summary>
        public const string TutorialLog = "SW.GMR.TutorialLog";
        /// <summary>SW.GMR.TutorialLog.Open.</summary>
        public const string TutorialLogOpen = "SW.GMR.TutorialLog.Open";
        /// <summary>SW.GMR.TutorialLog.Open.Confirmation.</summary>
        public const string TutorialLogOpenConfirmation = "SW.GMR.TutorialLog.Open.Confirmation";
        /// <summary>SW.GMR.TutorialLog.Open.Request.</summary>
        public const string TutorialLogOpenRequest = "SW.GMR.TutorialLog.Open.Request";
        /// <summary>SW.GMR.UI.</summary>
        public const string UI = "SW.GMR.UI";
        /// <summary>SW.GMR.UI.Action.</summary>
        public const string UIAction = "SW.GMR.UI.Action";
        /// <summary>SW.GMR.UI.Action.Dispatched.</summary>
        public const string UIActionDispatched = "SW.GMR.UI.Action.Dispatched";
        /// <summary>SW.GMR.UI.AsyncTask.</summary>
        public const string UIAsyncTask = "SW.GMR.UI.AsyncTask";
        /// <summary>SW.GMR.UI.AsyncTask.Completed. Payload: <see cref="global::UE.SWCoreTransitionScreen.FOnlineAsyncTaskMessage"/>.</summary>
        public const string UIAsyncTaskCompleted = "SW.GMR.UI.AsyncTask.Completed";
        /// <summary>SW.GMR.UI.AsyncTask.Queued. Payload: <see cref="global::UE.SWCoreTransitionScreen.FOnlineAsyncTaskMessage"/>.</summary>
        public const string UIAsyncTaskQueued = "SW.GMR.UI.AsyncTask.Queued";
        /// <summary>SW.GMR.UI.Companion.</summary>
        public const string UICompanion = "SW.GMR.UI.Companion";
        /// <summary>SW.GMR.UI.Companion.Teleport.</summary>
        public const string UICompanionTeleport = "SW.GMR.UI.Companion.Teleport";
        /// <summary>SW.GMR.UI.Conflicting.</summary>
        public const string UIConflicting = "SW.GMR.UI.Conflicting";
        /// <summary>SW.GMR.UI.Conflicting.Save.</summary>
        public const string UIConflictingSave = "SW.GMR.UI.Conflicting.Save";
        /// <summary>SW.GMR.UI.Conflicting.Save.Dialog.</summary>
        public const string UIConflictingSaveDialog = "SW.GMR.UI.Conflicting.Save.Dialog";
        /// <summary>SW.GMR.UI.Conflicting.Save.Dialog.Prompt.</summary>
        public const string UIConflictingSaveDialogPrompt = "SW.GMR.UI.Conflicting.Save.Dialog.Prompt";
        /// <summary>SW.GMR.UI.Conflicting.Save.Dialog.Prompt.Result.</summary>
        public const string UIConflictingSaveDialogPromptResult = "SW.GMR.UI.Conflicting.Save.Dialog.Prompt.Result";
        /// <summary>SW.GMR.UI.Cutscene.</summary>
        public const string UICutscene = "SW.GMR.UI.Cutscene";
        /// <summary>SW.GMR.UI.Cutscene.Delay.</summary>
        public const string UICutsceneDelay = "SW.GMR.UI.Cutscene.Delay";
        /// <summary>SW.GMR.UI.Cutscene.Delay.Complete.</summary>
        public const string UICutsceneDelayComplete = "SW.GMR.UI.Cutscene.Delay.Complete";
        /// <summary>SW.GMR.UI.Cutscene.Delay.Start.</summary>
        public const string UICutsceneDelayStart = "SW.GMR.UI.Cutscene.Delay.Start";
        /// <summary>SW.GMR.UI.Dialog.</summary>
        public const string UIDialog = "SW.GMR.UI.Dialog";
        /// <summary>SW.GMR.UI.Dialog.Alert.</summary>
        public const string UIDialogAlert = "SW.GMR.UI.Dialog.Alert";
        /// <summary>SW.GMR.UI.Dialog.Alert.Show.</summary>
        public const string UIDialogAlertShow = "SW.GMR.UI.Dialog.Alert.Show";
        /// <summary>SW.GMR.UI.Dialog.Content.</summary>
        public const string UIDialogContent = "SW.GMR.UI.Dialog.Content";
        /// <summary>SW.GMR.UI.Dialog.Content.Show.</summary>
        public const string UIDialogContentShow = "SW.GMR.UI.Dialog.Content.Show";
        /// <summary>SW.GMR.UI.Dialog.Simple.</summary>
        public const string UIDialogSimple = "SW.GMR.UI.Dialog.Simple";
        /// <summary>SW.GMR.UI.Dialog.Simple.Modal.</summary>
        public const string UIDialogSimpleModal = "SW.GMR.UI.Dialog.Simple.Modal";
        /// <summary>SW.GMR.UI.Dialog.Simple.Modal.Show.</summary>
        public const string UIDialogSimpleModalShow = "SW.GMR.UI.Dialog.Simple.Modal.Show";
        /// <summary>SW.GMR.UI.Difficulty.</summary>
        public const string UIDifficulty = "SW.GMR.UI.Difficulty";
        /// <summary>SW.GMR.UI.Difficulty.Open.</summary>
        public const string UIDifficultyOpen = "SW.GMR.UI.Difficulty.Open";
        /// <summary>SW.GMR.UI.Emote.</summary>
        public const string UIEmote = "SW.GMR.UI.Emote";
        /// <summary>SW.GMR.UI.Emote.Performed.</summary>
        public const string UIEmotePerformed = "SW.GMR.UI.Emote.Performed";
        /// <summary>SW.GMR.UI.EventLog.</summary>
        public const string UIEventLog = "SW.GMR.UI.EventLog";
        /// <summary>SW.GMR.UI.EventLog.Close.</summary>
        public const string UIEventLogClose = "SW.GMR.UI.EventLog.Close";
        /// <summary>SW.GMR.UI.EventLog.NewMessage.</summary>
        public const string UIEventLogNewMessage = "SW.GMR.UI.EventLog.NewMessage";
        /// <summary>SW.GMR.UI.EventLog.Open.</summary>
        public const string UIEventLogOpen = "SW.GMR.UI.EventLog.Open";
        /// <summary>SW.GMR.UI.EventLog.RemoveOldest.</summary>
        public const string UIEventLogRemoveOldest = "SW.GMR.UI.EventLog.RemoveOldest";
        /// <summary>SW.GMR.UI.EventLog.ToggleActive.</summary>
        public const string UIEventLogToggleActive = "SW.GMR.UI.EventLog.ToggleActive";
        /// <summary>SW.GMR.UI.Game.</summary>
        public const string UIGame = "SW.GMR.UI.Game";
        /// <summary>SW.GMR.UI.Game.Mode.</summary>
        public const string UIGameMode = "SW.GMR.UI.Game.Mode";
        /// <summary>SW.GMR.UI.Game.Mode.Select.</summary>
        public const string UIGameModeSelect = "SW.GMR.UI.Game.Mode.Select";
        /// <summary>SW.GMR.UI.Game.Mode.Select.Result.</summary>
        public const string UIGameModeSelectResult = "SW.GMR.UI.Game.Mode.Select.Result";
        /// <summary>SW.GMR.UI.InGameNotifications.</summary>
        public const string UIInGameNotifications = "SW.GMR.UI.InGameNotifications";
        /// <summary>SW.GMR.UI.InGameNotifications.Player.</summary>
        public const string UIInGameNotificationsPlayer = "SW.GMR.UI.InGameNotifications.Player";
        /// <summary>SW.GMR.UI.InGameNotifications.Player.LevelUp.</summary>
        public const string UIInGameNotificationsPlayerLevelUp = "SW.GMR.UI.InGameNotifications.Player.LevelUp";
        /// <summary>SW.GMR.UI.InGameNotifications.Player.ThreatIncreased.</summary>
        public const string UIInGameNotificationsPlayerThreatIncreased = "SW.GMR.UI.InGameNotifications.Player.ThreatIncreased";
        /// <summary>SW.GMR.UI.Indicator.</summary>
        public const string UIIndicator = "SW.GMR.UI.Indicator";
        /// <summary>SW.GMR.UI.Indicator.Add. Payload: <see cref="global::UE.SpicewoodUI.FIndicatorAddPayload"/>.</summary>
        public const string UIIndicatorAdd = "SW.GMR.UI.Indicator.Add";
        /// <summary>SW.GMR.UI.Indicator.Discover.</summary>
        public const string UIIndicatorDiscover = "SW.GMR.UI.Indicator.Discover";
        /// <summary>SW.GMR.UI.Indicator.Remove.</summary>
        public const string UIIndicatorRemove = "SW.GMR.UI.Indicator.Remove";
        /// <summary>SW.GMR.UI.ItemCollection.</summary>
        public const string UIItemCollection = "SW.GMR.UI.ItemCollection";
        /// <summary>SW.GMR.UI.ItemCollection.Completed.</summary>
        public const string UIItemCollectionCompleted = "SW.GMR.UI.ItemCollection.Completed";
        /// <summary>SW.GMR.UI.NPCDialogue.</summary>
        public const string UINPCDialogue = "SW.GMR.UI.NPCDialogue";
        /// <summary>SW.GMR.UI.Network.</summary>
        public const string UINetwork = "SW.GMR.UI.Network";
        /// <summary>SW.GMR.UI.Network.Disconnected.</summary>
        public const string UINetworkDisconnected = "SW.GMR.UI.Network.Disconnected";
        /// <summary>SW.GMR.UI.Network.Disconnected.Dialog.</summary>
        public const string UINetworkDisconnectedDialog = "SW.GMR.UI.Network.Disconnected.Dialog";
        /// <summary>SW.GMR.UI.OnPrimaryPlayerHashSet. Payload: <see cref="global::UE.SWCorePayloads.FOnPrimaryPlayerHashSetPayload"/>.</summary>
        public const string UIOnPrimaryPlayerHashSet = "SW.GMR.UI.OnPrimaryPlayerHashSet";
        /// <summary>SW.GMR.UI.OndoorMarkersRequestCompleted. Payload: <see cref="global::UE.Minimap.FDoorContentsPayload"/>.</summary>
        public const string UIOndoorMarkersRequestCompleted = "SW.GMR.UI.OndoorMarkersRequestCompleted";
        /// <summary>SW.GMR.UI.Online.</summary>
        public const string UIOnline = "SW.GMR.UI.Online";
        /// <summary>SW.GMR.UI.Online.Online.</summary>
        public const string UIOnlineOnline = "SW.GMR.UI.Online.Online";
        /// <summary>SW.GMR.UI.Online.Online.Start.</summary>
        public const string UIOnlineOnlineStart = "SW.GMR.UI.Online.Online.Start";
        /// <summary>SW.GMR.UI.Online.Online.Start.Game.</summary>
        public const string UIOnlineOnlineStartGame = "SW.GMR.UI.Online.Online.Start.Game";
        /// <summary>SW.GMR.UI.Online.Online.Start.Game.Aborted.</summary>
        public const string UIOnlineOnlineStartGameAborted = "SW.GMR.UI.Online.Online.Start.Game.Aborted";
        /// <summary>SW.GMR.UI.Online.Online.Start.Game.Completed.</summary>
        public const string UIOnlineOnlineStartGameCompleted = "SW.GMR.UI.Online.Online.Start.Game.Completed";
        /// <summary>SW.GMR.UI.Open.</summary>
        public const string UIOpen = "SW.GMR.UI.Open";
        /// <summary>SW.GMR.UI.Open.PressAnyKey.</summary>
        public const string UIOpenPressAnyKey = "SW.GMR.UI.Open.PressAnyKey";
        /// <summary>SW.GMR.UI.Overindulgence.</summary>
        public const string UIOverindulgence = "SW.GMR.UI.Overindulgence";
        /// <summary>SW.GMR.UI.Overindulgence.Notification.</summary>
        public const string UIOverindulgenceNotification = "SW.GMR.UI.Overindulgence.Notification";
        /// <summary>SW.GMR.UI.Ping.</summary>
        public const string UIPing = "SW.GMR.UI.Ping";
        /// <summary>SW.GMR.UI.Ping.Performed.</summary>
        public const string UIPingPerformed = "SW.GMR.UI.Ping.Performed";
        /// <summary>SW.GMR.UI.Player.</summary>
        public const string UIPlayer = "SW.GMR.UI.Player";
        /// <summary>SW.GMR.UI.Player.Died.</summary>
        public const string UIPlayerDied = "SW.GMR.UI.Player.Died";
        /// <summary>SW.GMR.UI.Player.Teleported.</summary>
        public const string UIPlayerTeleported = "SW.GMR.UI.Player.Teleported";
        /// <summary>SW.GMR.UI.PlayerMap.</summary>
        public const string UIPlayerMap = "SW.GMR.UI.PlayerMap";
        /// <summary>SW.GMR.UI.PlayerMap.IsGuest.</summary>
        public const string UIPlayerMapIsGuest = "SW.GMR.UI.PlayerMap.IsGuest";
        /// <summary>SW.GMR.UI.SingleOndoorMarkerRequestCompleted. Payload: <see cref="global::UE.Minimap.FDoorContentsSinglePayload"/>.</summary>
        public const string UISingleOndoorMarkerRequestCompleted = "SW.GMR.UI.SingleOndoorMarkerRequestCompleted";
        /// <summary>SW.GMR.UI.Talisman.</summary>
        public const string UITalisman = "SW.GMR.UI.Talisman";
        /// <summary>SW.GMR.UI.Talisman.LevelUp.</summary>
        public const string UITalismanLevelUp = "SW.GMR.UI.Talisman.LevelUp";
        /// <summary>SW.GMR.UI.Talisman.XPGained.</summary>
        public const string UITalismanXPGained = "SW.GMR.UI.Talisman.XPGained";
        /// <summary>SW.GMR.UI.Teleport.</summary>
        public const string UITeleport = "SW.GMR.UI.Teleport";
        /// <summary>SW.GMR.UI.TutorialLog.</summary>
        public const string UITutorialLog = "SW.GMR.UI.TutorialLog";
        /// <summary>SW.GMR.UI.TutorialLog.Currency.</summary>
        public const string UITutorialLogCurrency = "SW.GMR.UI.TutorialLog.Currency";
        /// <summary>SW.GMR.UI.Vendor.</summary>
        public const string UIVendor = "SW.GMR.UI.Vendor";
        /// <summary>SW.GMR.UI.Vendor.LevelUp.</summary>
        public const string UIVendorLevelUp = "SW.GMR.UI.Vendor.LevelUp";
        /// <summary>SW.GMR.UI.Vendor.VillageMerchant.</summary>
        public const string UIVendorVillageMerchant = "SW.GMR.UI.Vendor.VillageMerchant";
        /// <summary>SW.GMR.UI.Vendor.VillageMerchant.ItemsGenerated.</summary>
        public const string UIVendorVillageMerchantItemsGenerated = "SW.GMR.UI.Vendor.VillageMerchant.ItemsGenerated";
        /// <summary>SW.GMR.UI.Vendor.VillageMerchant.RefreshCompleted.</summary>
        public const string UIVendorVillageMerchantRefreshCompleted = "SW.GMR.UI.Vendor.VillageMerchant.RefreshCompleted";
        /// <summary>SW.GMR.UI.Vignette.</summary>
        public const string UIVignette = "SW.GMR.UI.Vignette";
        /// <summary>SW.GMR.UI.Vignette.Message.</summary>
        public const string UIVignetteMessage = "SW.GMR.UI.Vignette.Message";
        /// <summary>SW.GMR.UI.Vignette.Message.Start.</summary>
        public const string UIVignetteMessageStart = "SW.GMR.UI.Vignette.Message.Start";
        /// <summary>SW.GMR.UI.Vignette.Message.Stop.</summary>
        public const string UIVignetteMessageStop = "SW.GMR.UI.Vignette.Message.Stop";
        /// <summary>SW.GMR.UI.Vignette.Type.</summary>
        public const string UIVignetteType = "SW.GMR.UI.Vignette.Type";
        /// <summary>SW.GMR.UI.Vignette.Type.Enderman.</summary>
        public const string UIVignetteTypeEnderman = "SW.GMR.UI.Vignette.Type.Enderman";
        /// <summary>SW.GMR.UI.Vignette.Type.SoulConstruct.</summary>
        public const string UIVignetteTypeSoulConstruct = "SW.GMR.UI.Vignette.Type.SoulConstruct";
        /// <summary>SW.GMR.UI.Vignette.Type.Warden.</summary>
        public const string UIVignetteTypeWarden = "SW.GMR.UI.Vignette.Type.Warden";
        /// <summary>SW.GMR.User.</summary>
        public const string User = "SW.GMR.User";
        /// <summary>SW.GMR.User.Local.</summary>
        public const string UserLocal = "SW.GMR.User.Local";
        /// <summary>SW.GMR.User.Local.Abort.</summary>
        public const string UserLocalAbort = "SW.GMR.User.Local.Abort";
        /// <summary>SW.GMR.User.Local.Added.</summary>
        public const string UserLocalAdded = "SW.GMR.User.Local.Added";
        /// <summary>SW.GMR.User.Local.Join. Payload: <see cref="global::UE.PlayerEvents.FPlayerEventLocalUserJoinMessage"/>.</summary>
        public const string UserLocalJoin = "SW.GMR.User.Local.Join";
        /// <summary>SW.GMR.User.Local.Leave.</summary>
        public const string UserLocalLeave = "SW.GMR.User.Local.Leave";
        /// <summary>SW.GMR.User.Local.Leave.Start.</summary>
        public const string UserLocalLeaveStart = "SW.GMR.User.Local.Leave.Start";
        /// <summary>SW.GMR.User.Logged.</summary>
        public const string UserLogged = "SW.GMR.User.Logged";
        /// <summary>SW.GMR.User.Logged.In.</summary>
        public const string UserLoggedIn = "SW.GMR.User.Logged.In";
        /// <summary>SW.GMR.User.Online.</summary>
        public const string UserOnline = "SW.GMR.User.Online";
        /// <summary>SW.GMR.User.Online.Join.</summary>
        public const string UserOnlineJoin = "SW.GMR.User.Online.Join";
        /// <summary>SW.GMR.User.Online.Joining.</summary>
        public const string UserOnlineJoining = "SW.GMR.User.Online.Joining";
        /// <summary>SW.GMR.User.Online.Joining.Party.</summary>
        public const string UserOnlineJoiningParty = "SW.GMR.User.Online.Joining.Party";
        /// <summary>SW.GMR.User.Online.Joining.Party.Notification.</summary>
        public const string UserOnlineJoiningPartyNotification = "SW.GMR.User.Online.Joining.Party.Notification";
        /// <summary>SW.GMR.User.Online.Leave.</summary>
        public const string UserOnlineLeave = "SW.GMR.User.Online.Leave";
        /// <summary>SW.GMR.User.Online.Leaving.</summary>
        public const string UserOnlineLeaving = "SW.GMR.User.Online.Leaving";
        /// <summary>SW.GMR.User.Online.Leaving.Party.</summary>
        public const string UserOnlineLeavingParty = "SW.GMR.User.Online.Leaving.Party";
        /// <summary>SW.GMR.User.Online.Leaving.Party.Notification.</summary>
        public const string UserOnlineLeavingPartyNotification = "SW.GMR.User.Online.Leaving.Party.Notification";
        /// <summary>SW.GMR.User.Online.NewLeader.</summary>
        public const string UserOnlineNewLeader = "SW.GMR.User.Online.NewLeader";
        /// <summary>SW.GMR.User.Party.</summary>
        public const string UserParty = "SW.GMR.User.Party";
        /// <summary>SW.GMR.User.Party.Join. Payload: <see cref="global::UE.PlayerEvents.FPlayerEventPartyUserJoinMessage"/>.</summary>
        public const string UserPartyJoin = "SW.GMR.User.Party.Join";
        /// <summary>SW.GMR.User.Party.Join.Via.</summary>
        public const string UserPartyJoinVia = "SW.GMR.User.Party.Join.Via";
        /// <summary>SW.GMR.User.Party.Join.Via.Out.</summary>
        public const string UserPartyJoinViaOut = "SW.GMR.User.Party.Join.Via.Out";
        /// <summary>SW.GMR.User.Party.Join.Via.Out.Of.</summary>
        public const string UserPartyJoinViaOutOf = "SW.GMR.User.Party.Join.Via.Out.Of";
        /// <summary>SW.GMR.User.Party.Join.Via.Out.Of.Game.</summary>
        public const string UserPartyJoinViaOutOfGame = "SW.GMR.User.Party.Join.Via.Out.Of.Game";
        /// <summary>SW.GMR.User.Party.Join.Via.Out.Of.Game.Invite.</summary>
        public const string UserPartyJoinViaOutOfGameInvite = "SW.GMR.User.Party.Join.Via.Out.Of.Game.Invite";
        /// <summary>SW.GMR.User.Party.Join.Via.Out.Of.Game.Invite.When.</summary>
        public const string UserPartyJoinViaOutOfGameInviteWhen = "SW.GMR.User.Party.Join.Via.Out.Of.Game.Invite.When";
        /// <summary>SW.GMR.User.Party.Join.Via.Out.Of.Game.Invite.When.Offline.</summary>
        public const string UserPartyJoinViaOutOfGameInviteWhenOffline = "SW.GMR.User.Party.Join.Via.Out.Of.Game.Invite.When.Offline";
        /// <summary>SW.GMR.User.Party.Leave.</summary>
        public const string UserPartyLeave = "SW.GMR.User.Party.Leave";
        /// <summary>SW.GMR.World.</summary>
        public const string World = "SW.GMR.World";
        /// <summary>SW.GMR.World.Reset.</summary>
        public const string WorldReset = "SW.GMR.World.Reset";
        /// <summary>SW.GMR.World.Transition.</summary>
        public const string WorldTransition = "SW.GMR.World.Transition";
        /// <summary>SW.GMR.World.Transition.Begin. Payload: <see cref="global::UE.SWCoreTransitionScreen.FWorldTransitionMessage"/>.</summary>
        public const string WorldTransitionBegin = "SW.GMR.World.Transition.Begin";
        /// <summary>SW.GMR.World.Transition.End. Payload: <see cref="global::UE.SWCoreTransitionScreen.FWorldTransitionMessage"/>.</summary>
        public const string WorldTransitionEnd = "SW.GMR.World.Transition.End";
    }
}
