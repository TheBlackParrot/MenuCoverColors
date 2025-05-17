using JetBrains.Annotations;
using MenuCoverColors.Managers;
using MenuCoverColors.UI;
using Zenject;

namespace MenuCoverColors.Installers;

[UsedImplicitly]
internal class MenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<SettingsMenuManager>().AsSingle();
        
        Container.BindInterfacesTo<MenuColorManager>().AsSingle();
        Container.BindInterfacesTo<TransitionManager>().AsSingle();
    }
}