﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using GitClientVS.Contracts.Interfaces;
using GitClientVS.Contracts.Interfaces.Services;
using GitClientVS.Contracts.Interfaces.ViewModels;
using GitClientVS.Contracts.Interfaces.Views;
using GitClientVS.Infrastructure.ViewModels;
using GitClientVS.VisualStudio.UI.Window;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ReactiveUI;
using GitClientVS.Infrastructure.Extensions;

namespace GitClientVS.VisualStudio.UI.Services
{
    [Export(typeof(ICommandsService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class CommandsService : ICommandsService
    {
        private readonly ExportFactory<IDiffWindowControlViewModel> _diffFactory;
        private readonly ExportFactory<IPullRequestsWindowContainerViewModel> _pqFactory;
        private readonly IPageNavigationService<IPullRequestsWindow> _navigationService;
        private Package _package;

        [ImportingConstructor]
        public CommandsService(
            ExportFactory<IDiffWindowControlViewModel> diffFactory,
            ExportFactory<IPullRequestsWindowContainerViewModel> pqFactory,
            IPageNavigationService<IPullRequestsWindow> navigationService)
        {
            _diffFactory = diffFactory;
            _pqFactory = pqFactory;
            _navigationService = navigationService;
        }

        public void Initialize(object package)
        {
            _package = (Package)package;
        }


        public void ShowPullRequestsWindow()
        {
            var window = ShowWindow<PullRequestsWindow>();
            var vm = _pqFactory.CreateExport().Value;
            var view = window.Content as IPullRequestsWindowContainer;
            view.DataContext = vm;
            view.Window = window;
            vm.Initialize();
            _navigationService.ClearNavigationHistory();
            _navigationService.Navigate<IPullRequestsMainView>();
        }

        public void ShowDiffWindow(object parameter, int id)
        {
            var window = ShowWindow<DiffWindow>(id);
            var vm = (DiffWindowControlViewModel)_diffFactory.CreateExport().Value;
            var view = window.Content as IView;
            view.DataContext = vm;
            vm.Initialize(parameter);
            vm.WhenAnyValue(x => x.FileDiff).Where(x => x != null).Subscribe(x => window.Caption = $"Diff ({x.DisplayFileName})");
        }

        private TWindow ShowWindow<TWindow>(int id = 0) where TWindow : class
        {
            if (_package == null)
                throw new Exception("Package wasn't initialized");

            ToolWindowPane window = _package.FindToolWindow(typeof(TWindow), id, true);

            if (window?.Frame == null)
                throw new NotSupportedException("Cannot create window");

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

            return window as TWindow;
        }
    }
}
