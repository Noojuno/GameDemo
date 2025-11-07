namespace GameDemo;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

public interface IMultiplayerMenu : ICanvasLayer
{
  void ShowMenu();
  void HideMenu();
}

[Meta(typeof(IAutoNode))]
public partial class MultiplayerMenu : CanvasLayer, IMultiplayerMenu
{
  public override void _Notification(int what) => this.Notify(what);

  #region Dependencies

  [Dependency]
  public IMultiplayerRepo MultiplayerRepo => this.DependOn<IMultiplayerRepo>();

  [Dependency]
  public IMultiplayerLogic MultiplayerLogic => this.DependOn<IMultiplayerLogic>();

  #endregion Dependencies

  #region Nodes

  [Node("%HostButton")]
  public IButton HostButton { get; set; } = default!;

  [Node("%JoinButton")]
  public IButton JoinButton { get; set; } = default!;

  [Node("%DisconnectButton")]
  public IButton DisconnectButton { get; set; } = default!;

  [Node("%AddressLineEdit")]
  public ILineEdit AddressLineEdit { get; set; } = default!;

  [Node("%PortSpinBox")]
  public ISpinBox PortSpinBox { get; set; } = default!;

  [Node("%StatusLabel")]
  public ILabel StatusLabel { get; set; } = default!;

  [Node("%PlayerList")]
  public IItemList PlayerList { get; set; } = default!;

  [Node("%CloseButton")]
  public IButton CloseButton { get; set; } = default!;

  #endregion Nodes

  public void Setup()
  {
  }

  public void OnReady()
  {
    HostButton.Pressed += OnHostButtonPressed;
    JoinButton.Pressed += OnJoinButtonPressed;
    DisconnectButton.Pressed += OnDisconnectButtonPressed;
    CloseButton.Pressed += HideMenu;

    AddressLineEdit.Text = "127.0.0.1";
    PortSpinBox.Value = 7777;

    Hide();
  }

  public void OnResolved()
  {
    MultiplayerRepo.IsOnline.Sync += OnIsOnlineChanged;
    MultiplayerRepo.PeerConnected += OnPeerConnected;
    MultiplayerRepo.PeerDisconnected += OnPeerDisconnected;

    UpdateUI();
  }

  public void ShowMenu()
  {
    Show();
    UpdateUI();
  }

  public void HideMenu()
  {
    Hide();
  }

  private void OnHostButtonPressed()
  {
    var port = (int)PortSpinBox.Value;
    MultiplayerLogic.Input(new MultiplayerLogic.Input.HostGame(port));
    StatusLabel.Text = $"Hosting on port {port}";
  }

  private void OnJoinButtonPressed()
  {
    var address = AddressLineEdit.Text;
    var port = (int)PortSpinBox.Value;

    MultiplayerLogic.Input(new MultiplayerLogic.Input.JoinGame(address, port));
    StatusLabel.Text = $"Connecting to {address}:{port}";
  }

  private void OnDisconnectButtonPressed()
  {
    MultiplayerLogic.Input(new MultiplayerLogic.Input.Disconnect());
    StatusLabel.Text = "Disconnected";
  }

  private void OnIsOnlineChanged(bool isOnline)
  {
    CallDeferred(nameof(UpdateUI));
  }

  private void OnPeerConnected(int peerId)
  {
    CallDeferred(nameof(UpdatePlayerList));
  }

  private void OnPeerDisconnected(int peerId)
  {
    CallDeferred(nameof(UpdatePlayerList));
  }

  private void UpdateUI()
  {
    var isOnline = MultiplayerRepo.IsOnline.Value;
    
    HostButton.Disabled = isOnline;
    JoinButton.Disabled = isOnline;
    DisconnectButton.Disabled = !isOnline;
    AddressLineEdit.Editable = !isOnline;
    PortSpinBox.Editable = !isOnline;

    if (isOnline)
    {
      if (MultiplayerRepo.IsHosting.Value)
      {
        StatusLabel.Text = $"Hosting (Peer ID: {MultiplayerRepo.LocalPeerId.Value})";
      }
      else if (MultiplayerRepo.IsClient.Value)
      {
        StatusLabel.Text = $"Connected (Peer ID: {MultiplayerRepo.LocalPeerId.Value})";
      }
    }
    else
    {
      StatusLabel.Text = "Offline";
    }

    UpdatePlayerList();
  }

  private void UpdatePlayerList()
  {
    PlayerList.Clear();
    
    foreach (var peer in MultiplayerRepo.Peers.Values)
    {
      PlayerList.AddItem($"{peer.PlayerName} (ID: {peer.PeerId})");
    }
  }

  public void OnExitTree()
  {
    HostButton.Pressed -= OnHostButtonPressed;
    JoinButton.Pressed -= OnJoinButtonPressed;
    DisconnectButton.Pressed -= OnDisconnectButtonPressed;
    CloseButton.Pressed -= HideMenu;

    MultiplayerRepo.IsOnline.Sync -= OnIsOnlineChanged;
    MultiplayerRepo.PeerConnected -= OnPeerConnected;
    MultiplayerRepo.PeerDisconnected -= OnPeerDisconnected;
  }
}

