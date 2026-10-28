using NUnit.Framework;

public class KeyBindingsTests
{
    private string originalPrefix;

    [SetUp]
    public void SetUp()
    {
        originalPrefix = GameProgress.KeyPrefix;
        GameProgress.KeyPrefix = "GameCM.Tests.";
        KeyBindings.Reload();
        KeyBindings.ResetToDefaults();
    }

    [TearDown]
    public void TearDown()
    {
        KeyBindings.ResetToDefaults();
        GameProgress.KeyPrefix = originalPrefix;
        KeyBindings.Reload();
    }

    [Test]
    public void DefaultsAreWasdStyleLayout()
    {
        Assert.AreEqual("<Keyboard>/a", KeyBindings.Get(BindAction.Left));
        Assert.AreEqual("<Keyboard>/d", KeyBindings.Get(BindAction.Right));
        Assert.AreEqual("<Keyboard>/space", KeyBindings.Get(BindAction.Jump));
        Assert.AreEqual("<Keyboard>/j", KeyBindings.Get(BindAction.Punch));
        Assert.AreEqual("<Keyboard>/k", KeyBindings.Get(BindAction.Power));
        Assert.AreEqual("<Keyboard>/leftShift", KeyBindings.Get(BindAction.Sprint));
    }

    [Test]
    public void AssignSwapsKeysWhenAnotherActionAlreadyUsesTheKey()
    {
        string[] defaults = { "<Keyboard>/a", "<Keyboard>/d", "<Keyboard>/space", "<Keyboard>/j", "<Keyboard>/k", "<Keyboard>/leftShift" };

        string[] result = KeyBindings.Assign(defaults, BindAction.Punch, "<Keyboard>/k");

        Assert.AreEqual("<Keyboard>/k", result[(int)BindAction.Punch]);
        Assert.AreEqual("<Keyboard>/j", result[(int)BindAction.Power]);
        Assert.AreEqual("<Keyboard>/j", defaults[(int)BindAction.Punch], "o vetor original nao pode mudar");
    }

    [Test]
    public void AssignToANewKeyOnlyChangesThatAction()
    {
        string[] defaults = { "<Keyboard>/a", "<Keyboard>/d", "<Keyboard>/space", "<Keyboard>/j", "<Keyboard>/k", "<Keyboard>/leftShift" };

        string[] result = KeyBindings.Assign(defaults, BindAction.Jump, "<Keyboard>/w");

        Assert.AreEqual("<Keyboard>/w", result[(int)BindAction.Jump]);
        Assert.AreEqual("<Keyboard>/j", result[(int)BindAction.Punch]);
    }

    [Test]
    public void SetPersistsAcrossReload()
    {
        KeyBindings.Set(BindAction.Jump, "<Keyboard>/w");

        KeyBindings.Reload();

        Assert.AreEqual("<Keyboard>/w", KeyBindings.Get(BindAction.Jump));
    }

    [Test]
    public void EscapeCannotBeAssigned()
    {
        KeyBindings.Set(BindAction.Jump, "<Keyboard>/escape");

        Assert.AreEqual("<Keyboard>/space", KeyBindings.Get(BindAction.Jump));
        Assert.IsTrue(KeyBindings.IsReserved("<Keyboard>/escape"));
    }

    [Test]
    public void ResetToDefaultsRestoresEverything()
    {
        KeyBindings.Set(BindAction.Jump, "<Keyboard>/w");
        KeyBindings.Set(BindAction.Punch, "<Keyboard>/e");

        KeyBindings.ResetToDefaults();

        Assert.AreEqual("<Keyboard>/space", KeyBindings.Get(BindAction.Jump));
        Assert.AreEqual("<Keyboard>/j", KeyBindings.Get(BindAction.Punch));
    }

    [Test]
    public void DisplayNamesAreReadable()
    {
        Assert.AreEqual("Espaço", KeyBindings.DisplayName("<Keyboard>/space"));
        Assert.AreEqual("Shift esq.", KeyBindings.DisplayName("<Keyboard>/leftShift"));
        Assert.AreEqual("A", KeyBindings.DisplayName("<Keyboard>/a"));
        Assert.AreEqual("1", KeyBindings.DisplayName("<Keyboard>/digit1"));
        Assert.AreEqual("Num 5", KeyBindings.DisplayName("<Keyboard>/numpad5"));
        Assert.AreEqual("-", KeyBindings.DisplayName(null));
    }
}
