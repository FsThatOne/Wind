using FengZhi.Foundation.CharacterData;
using FengZhi.Vs;
using Godot;

namespace FengZhi.Ui;

public partial class AttributePanel : PanelContainer
{
	private Label _titleLabel = null!;
	private ProgressBar _hpBar = null!;
	private Label _hpLabel = null!;
	private ProgressBar _neixiBar = null!;
	private Label _neixiLabel = null!;
	private Label _strengthLabel = null!;
	private Label _agilityLabel = null!;
	private Label _innerPowerLabel = null!;
	private Label _insightLabel = null!;
	private Label _constitutionLabel = null!;
	private Label _attackLabel = null!;
	private Label _defenseLabel = null!;
	private Label _speedLabel = null!;
	private Label _critLabel = null!;
	private Label _totalPowerLabel = null!;

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("%TitleLabel");
		_hpBar = GetNode<ProgressBar>("%HpBar");
		_hpLabel = GetNode<Label>("%HpLabel");
		_neixiBar = GetNode<ProgressBar>("%NeixiBar");
		_neixiLabel = GetNode<Label>("%NeixiLabel");
		_strengthLabel = GetNode<Label>("%StrengthValue");
		_agilityLabel = GetNode<Label>("%AgilityValue");
		_innerPowerLabel = GetNode<Label>("%InnerPowerValue");
		_insightLabel = GetNode<Label>("%InsightValue");
		_constitutionLabel = GetNode<Label>("%ConstitutionValue");
		_attackLabel = GetNode<Label>("%AttackValue");
		_defenseLabel = GetNode<Label>("%DefenseValue");
		_speedLabel = GetNode<Label>("%SpeedValue");
		_critLabel = GetNode<Label>("%CritValue");
		_totalPowerLabel = GetNode<Label>("%TotalPowerValue");
	}

	public void Refresh(CharacterInstance player)
	{
		var attrs = player.Attributes;

		_titleLabel.Text = $"[DEV]【{attrs.TemplateId}】角色属性";

		var maxHp = player.GetMaxHp();
		_hpBar.MaxValue = maxHp;
		_hpBar.Value = attrs.CurrentHp;
		_hpLabel.Text = $"{attrs.CurrentHp} / {maxHp}";

		var maxNeixi = player.GetMaxNeiXi();
		_neixiBar.MaxValue = maxNeixi;
		_neixiBar.Value = attrs.CurrentNeiXi;
		_neixiLabel.Text = $"{attrs.CurrentNeiXi} / {maxNeixi}";

		_strengthLabel.Text = attrs.Strength.ToString();
		_agilityLabel.Text = attrs.Agility.ToString();
		_innerPowerLabel.Text = attrs.InnerPower.ToString();
		_insightLabel.Text = attrs.Insight.ToString();
		_constitutionLabel.Text = attrs.Constitution.ToString();

		_attackLabel.Text = player.GetAttackForType(MoveType.Rou).ToString();
		_defenseLabel.Text = player.GetDefense().ToString();
		_speedLabel.Text = player.GetSpeed().ToString();
		_critLabel.Text = $"{player.GetCritRate() * 100f:F1}%";
		_totalPowerLabel.Text = player.GetTotalPower().ToString();
	}

	public void Toggle()
	{
		Visible = !Visible;
		if (Visible)
		{
			var flow = GetNodeOrNull<JiangnanFlowController>("/root/JiangnanFlow");
			if (flow?.PlayerInstance != null)
				Refresh(flow.PlayerInstance);
		}
	}
}
