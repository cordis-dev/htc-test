using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Adroiti.SapphirePilot.Client.AspDotNetMvc.Controllers;
using Adroiti.SapphirePilot.Client.AspDotNetMvc.Features.RepositoryPages;
using Adroiti.SapphirePilot.Client.AspDotNetMvc.ViewModels;
using Adroiti.SapphirePilot.Core.Extensions;
using Adroiti.SapphirePilot.Core.Features.Branches;
using Adroiti.SapphirePilot.Core.Features.ChangeSets;
using Adroiti.SapphirePilot.Core.Features.CodeIssues;
using Adroiti.SapphirePilot.Core.Features.CodeLines;
using Adroiti.SapphirePilot.Core.Features.EntityStorage;
using Adroiti.SapphirePilot.Core.Features.PullRequests;
using Adroiti.SapphirePilot.Core.Features.RandomQuotes;
using Adroiti.SapphirePilot.Core.Features.Repositories;
using Adroiti.SapphirePilot.Core.Features.Serialization;
using Adroiti.SapphirePilot.Core.Tests.Infrastructure;
using Adroiti.Web.Infrastructure;
using Adroiti.Web.Infrastructure.Identity;
using Adroiti.Web.Infrastructure.Tests;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Adroiti.SapphirePilot.Client.AspDotNetMvc.Tests.Controllers
{
    [TestFixture]
    public class RepositoryControllerTests
    {
        private RepositoryController controller;
        private RepositoryPageContext context;
        private IRepositoryPageService pageService;
        private IJsonSerializer jsonSerializer;
        private SubstituteMapperService mapperService;
        private IRepositoryService repositoryService;
        private IModelStateErrorService modelStateErrorService;
        private IEntityStorage entityStorage;
        private IBranchService branchService;
        private IRepositoryControllerSecurityService securityService;
        private IIssuesService issuesService;
        private IAspNetIdentity aspNetIdentity;
        private ILocalUrlService localUrlService;

        [SetUp]
        public void Init()
        {
            aspNetIdentity = Substitute.For<IAspNetIdentity>();
            aspNetIdentity.UserKey.Returns("uid");
            pageService = Substitute.For<IRepositoryPageService>();
            jsonSerializer = Substitute.For<IJsonSerializer>();
            mapperService = new SubstituteMapperService();
            repositoryService = Substitute.For<IRepositoryService>();
            modelStateErrorService = Substitute.For<IModelStateErrorService>();
            entityStorage = Substitute.For<IEntityStorage>();
            branchService = Substitute.For<IBranchService>();
            securityService = Substitute.For<IRepositoryControllerSecurityService>();
            issuesService = Substitute.For<IIssuesService>();
            issuesService.GetByKeys(Arg.Any<HashSet<string>>()).Returns(new List<Issue>());
            localUrlService = Substitute.For<ILocalUrlService>();
            controller = new RepositoryController(pageService, mapperService, jsonSerializer, entityStorage,
                repositoryService, modelStateErrorService, branchService, securityService, localUrlService,
                issuesService, Substitute.For<IChangeSetService>(), Substitute.For<IRandomQuoteResolver>());
            context = new RepositoryPageContext { Branch = new Branch().New(), UrlKey = new RepositoryUrlKey() };
            controller.Url = Substitute.For<IUrlHelper>();
            HttpContext.Current = new HttpContext(new HttpRequest(null, "http://tempuri.org", null),
                new HttpResponse(null));
        }

        public class Class : SettingsControllerTests
        {
            [Test]
            public void HasRedirectAttribute()
            {
                typeof(RepositoryController).Should().BeDecoratedWith<RedirectIfBranchIsNotFoundAttribute>();
            }
        }

        public class Overview : RepositoryControllerTests
        {
            private RepositoryStatsHeaderViewModel statsHeader;

            [SetUp]
            public void InitTest()
            {
                context.Repository = new Repository().New();
                context.Branch = new Branch().New();
                statsHeader = new RepositoryStatsHeaderViewModel();
                pageService.GetStatsHeader(Arg.Any<GetStatsHeaderRequest>()).Returns(statsHeader);
                pageService.GetChangeSets(Arg.Any<GetChangeSetViewModelsRequest>())
                    .Returns(new ChangeSetViewModelResponse());
            }

            public RepositoryBranchOverviewViewModel Call()
            {
                var viewResult = (ViewResult)controller.Overview(context);
                return (RepositoryBranchOverviewViewModel)viewResult.Model;
            }

            [Test]
            public void ReturnsDefaultView()
            {
                var actionResult = controller.Overview(context);

                ActionResultAssert.IsViewOf<RepositoryBranchOverviewViewModel>(actionResult);
            }

            [Test]
            public void SetsHeader()
            {
                context.Header = new RepositoryHeaderViewModel();

                var viewModel = Call();

                Assert.AreEqual(context.Header, viewModel.Header);
            }

            [Test]
            public void SetsChangeSets()
            {
                var viewModels = new List<ChangeSetViewModel> { new ChangeSetViewModel() };
                var response = new ChangeSetViewModelResponse { List = viewModels };
                pageService.GetChangeSets(Arg.Any<GetChangeSetViewModelsRequest>()).Returns(response);

                var viewModel = Call();

                pageService.Received()
                    .GetChangeSets(Args.EquivalentTo<GetChangeSetViewModelsRequest>(context, options => options
                        .Excluding(request => request.PageSize)
                        .Excluding(request => request.PageNumber)));
                Assert.AreEqual(viewModels, viewModel.ClientModel.ChangeSets);
            }

            [Test]
            public void SetsHotspots()
            {
                var viewModels = new List<HotspotViewModel> { new HotspotViewModel() };
                pageService.GetHotspots(Arg.Any<GetHotspotsRequest>()).Returns(viewModels);

                var viewModel = Call();

                pageService.Received().GetHotspots(Args.EquivalentTo<GetHotspotsRequest>(context));
                Assert.AreEqual(viewModels, viewModel.Hotspots);
            }

            [Test]
            public void SetsLibraries()
            {
                var viewModels = new List<LibraryViewModel> { new LibraryViewModel() };
                pageService.GetLibraries(Arg.Any<GetLibrariesRequest>()).Returns(viewModels);

                var viewModel = Call();

                pageService.Received().GetLibraries(Args.EquivalentTo<GetLibrariesRequest>(context));
                Assert.AreEqual(viewModels, viewModel.ClientModel.Libraries);
            }
        }

        public class Files : RepositoryControllerTests
        {
            [SetUp]
            public void InitTest()
            {
                context.Repository = new Repository().New();
                context.Branch = new Branch().New();
                pageService.GetBranchFiles(Arg.Any<GetBranchFileViewModelsRequest>())
                    .Returns(new GetBranchFilesResponse());
            }

            public RepositoryBranchFilesViewModel Call()
            {
                var viewResult = (ViewResult)controller.Files(context);
                return (RepositoryBranchFilesViewModel)viewResult.Model;
            }

            [Test]
            public void ReturnsDefaultView()
            {
                var actionResult = controller.Files(context);

                ActionResultAssert.IsViewOf<RepositoryBranchFilesViewModel>(actionResult);
            }

            [Test]
            public void SetsHeader()
            {
                context.Header = new RepositoryHeaderViewModel();

                var viewModel = Call();

                Assert.AreEqual(context.Header, viewModel.Header);
            }

            [Test]
            public void SetsUrlHelper()
            {
                var viewModel = Call();

                Assert.AreEqual(controller.Url, viewModel.UrlHelper);
            }

            [Test]
            public void SetsLocalUrlService()
            {
                var viewModel = Call();

                Assert.AreEqual(localUrlService, viewModel.LocalUrlService);
            }

            [Test]
            public void SetsUrlKey()
            {
                var viewModel = Call();

                Assert.AreEqual(context.UrlKey, viewModel.UrlKey);
            }

            [Test]
            public void SetsGrid()
            {
                var queryable = Substitute.For<IQueryable<Core.Features.CodeFiles.CodeFile>>();
                branchService.GetActiveFilesQuery(context.Branch)
                    .Returns(queryable);

                var viewModel = Call();

                Assert.IsNotNull(viewModel.Grid);
                Assert.AreEqual(queryable, viewModel.Grid.GridItems);
            }
        }

        public class Issues : RepositoryControllerTests
        {
            [SetUp]
            public void InitTest()
            {
                context.Branch = new Branch().New();
                context.Repository = new Repository().New();
                pageService.GetBranchIssues(Arg.Any<GetBranchIssuesRequest>())
                    .Returns(new GetBranchIssuesResponse());
                pageService.GetBaseRepositoryIssues(Arg.Any<GetBaseRepositoryIssuesRequest>())
                    .Returns(new RepositoryIssuesViewModel
                             {
                                 ClientData = new RepositoryIssuesClientData()
                             });
            }

            public RepositoryIssuesViewModel Call()
            {
                var viewResult = (ViewResult)controller.Issues(context);
                return (RepositoryIssuesViewModel)viewResult.Model;
            }

            [Test]
            public void ReturnsDefaultView()
            {
                var actionResult = controller.Issues(context);

                ActionResultAssert.IsViewOf<RepositoryIssuesViewModel>(actionResult);
            }

            [Test]
            public void ReturnsViewModelFromPageServe()
            {
                var fileIssueViewModels = new List<CodeFileIssueViewModel>();
                pageService.GetBranchIssues(Arg.Any<GetBranchIssuesRequest>())
                    .Returns(new GetBranchIssuesResponse { List = fileIssueViewModels });
                var expected = new RepositoryFileViewModel { ClientData = new RepositoryIssuesClientData() };
                pageService.GetBaseRepositoryIssues(Arg.Any<GetBaseRepositoryIssuesRequest>()).Returns(expected);

                var viewModel = Call();

                pageService.Received().GetBaseRepositoryIssues(Arg.Is<GetBaseRepositoryIssuesRequest>(
                    request => request.Context == context && request.ViewModels == fileIssueViewModels));
                Assert.AreEqual(expected, viewModel);
            }
        }

        public class CodeFile : RepositoryControllerTests
        {
            [SetUp]
            public void InitTest()
            {
                context.Repository = new Repository().New();
                context.Branch = new Branch().New();
                context.UrlKey = new RepositoryUrlKey();
                context.Header = new RepositoryHeaderViewModel();
                pageService.GetFileDetail(Arg.Any<GetFileViewModelDetailRequest>())
                    .Returns(new RepositoryFileDetailViewModel());
                pageService.GetBaseRepositoryIssues(Arg.Any<GetBaseRepositoryIssuesRequest>())
                    .Returns(new RepositoryIssuesViewModel { ClientData = new RepositoryIssuesClientData() });
            }

            public RepositoryFileViewModel Call()
            {
                var viewResult = (ViewResult)controller.CodeFile(context);
                return (RepositoryFileViewModel)viewResult.Model;
            }

            [Test]
            public void ReturnsDefaultView()
            {
                var actionResult = controller.CodeFile(context);

                ActionResultAssert.IsViewOf<RepositoryFileViewModel>(actionResult);
            }

            [Test]
            public void ReturnsNotFoundIfDetailIsNull()
            {
                pageService.GetFileDetail(Arg.Any<GetFileViewModelDetailRequest>())
                    .Returns((RepositoryFileDetailViewModel)null);

                var actionResult = controller.CodeFile(context);

                ActionResultAssert.IsNotFound(actionResult);
            }

            [Test]
            public void ReturnsNotFoundIfFileIsHidden()
            {
                pageService.GetFileDetail(Arg.Any<GetFileViewModelDetailRequest>())
                    .Returns(new RepositoryFileDetailViewModel { IsHidden = true });

                var actionResult = controller.CodeFile(context);

                ActionResultAssert.IsNotFound(actionResult);
            }

            [Test]
            public void ReturnsIgnoredFileViewModelIfIgnored()
            {
                pageService.GetFileDetail(Arg.Any<GetFileViewModelDetailRequest>())
                    .Returns(new RepositoryFileDetailViewModel { IsIgnored = true });
                context.Header.IgnoreLink = new PageLink();

                var actionResult = controller.CodeFile(context);

                ActionResultAssert.IsNamedViewOf<IgnoredFileViewModel>(MVC.Repository.Views.IgnoredCodeFile,
                    actionResult);
                var viewResult = (ViewResult)actionResult;
                var viewModel = (IgnoredFileViewModel)viewResult.Model;
                Assert.AreEqual(context.Header, viewModel.Header);
                Assert.AreEqual(context.Header.IgnoreLink, viewModel.EditExcludePatternsLink);
            }

            [Test]
            public void SetsFilesUrlAsActiveUrlForHeaderWhenIgnored()
            {
                pageService.GetFileDetail(Arg.Any<GetFileViewModelDetailRequest>())
                    .Returns(new RepositoryFileDetailViewModel { IsIgnored = true });

                context.Header.FilesUrl = "f-url";

                controller.CodeFile(context);

                Assert.AreEqual(context.Header.FilesUrl, context.Header.ActiveUrl);
            }

            [Test]
            public void SetsFilesUrlAsActiveUrlForHeaderWhenNotIgnored()
            {
                pageService.GetFileDetail(Arg.Any<GetFileViewModelDetailRequest>())
                    .Returns(new RepositoryFileDetailViewModel { IsIgnored = false });

                context.Header.FilesUrl = "f-url";

                controller.CodeFile(context);

                Assert.AreEqual(context.Header.FilesUrl, context.Header.ActiveUrl);
            }

            [Test]
            public void SetsFileDetail()
            {
                var detailViewModel = Builder.Create<RepositoryFileDetailViewModel>();
                pageService.GetFileDetail(Arg.Any<GetFileViewModelDetailRequest>())
                    .Returns(detailViewModel);

                var viewModel = Call();

                pageService.Received()
                    .GetFileDetail(Args.EquivalentTo<GetFileViewModelDetailRequest>(context, options => options
                        .Excluding(request => request.IncludeContent)));
                Assert.AreEqual(detailViewModel, viewModel.Detail);
            }

            [Test]
            public void SetsIgnoreFileProperties()
            {
                var expected = MVC.Repository.IgnoreCodeFile();
                securityService.IsAllowed(Arg.Is<ActionResult>(result => result.Matches(expected)),
                    context.RepositoryRole)
                    .Returns(true);

                var viewModel = Call();

                viewModel.IgnoreFileAction.Matches(expected);
                Assert.AreEqual(true, viewModel.IgnoreFileButtonVisible);
            }
        }

        public class Branches : RepositoryControllerTests
        {
            [SetUp]
            public void InitTest()
            {
                context.Repository = new Repository().New();
                context.Branch = new Branch().New();
            }

            public RepositoryBranchesViewModel Call()
            {
                var viewResult = (ViewResult)controller.Branches(context);
                return (RepositoryBranchesViewModel)viewResult.Model;
            }

            [Test]
            public void ReturnsDefaultView()
            {
                var actionResult = controller.Branches(context);

                ActionResultAssert.IsViewOf<RepositoryBranchesViewModel>(actionResult);
            }

            [Test]
            public void SetsHeader()
            {
                context.Header = new RepositoryHeaderViewModel();

                var viewModel = Call();

                Assert.AreEqual(context.Header, viewModel.Header);
            }

            [Test]
            public void SetsBranchList()
            {
                var viewModels = new List<RepositoryBranchViewModel> { new RepositoryBranchViewModel() };
                pageService.GetBranches(Arg.Any<GetBranchViewModelsRequest>()).Returns(viewModels);
                jsonSerializer.Serialize(viewModels).Returns("serialized");

                var viewModel = Call();

                pageService.Received().GetBranches(Args.EquivalentTo<GetBranchViewModelsRequest>(context));
                Assert.AreEqual(viewModels, viewModel.List);
                Assert.AreEqual("serialized", viewModel.ListAsJson);
            }

            [Test]
            public void SetsToggleAnalysisLink()
            {
                var pageLink = new PageLink();
                securityService.GetPageLink(Arg.Any<GetPageLinkRequest>()).Returns(pageLink);
                jsonSerializer.Serialize(pageLink).Returns("s");

                var viewModel = Call();

                securityService.Received().GetPageLink(Args.EquivalentTo<GetPageLinkRequest>(context,
                    options => options.Excluding(request => request.ActionResult)));
                securityService.Received().GetPageLink(Arg.Is<GetPageLinkRequest>(
                    request => request.ActionResult.Matches(MVC.Repository.ToggleBranchAnalysis())));
                Assert.AreEqual(pageLink, viewModel.ToggleAnalysisLink);
                Assert.AreEqual("s", viewModel.ToggleAnalysisLinkAsJson);
            }

            [Test]
            public void SetsExternalId()
            {
                context.Repository.ExternalId = "id";

                var viewModel = Call();

                Assert.AreEqual("id", viewModel.RepositoryExternalId);
            }
        }

        public class Pulls : RepositoryControllerTests
        {
            [SetUp]
            public void InitTest()
            {
                context.Repository = new Repository().New();
                context.Branch = new Branch().New();
            }

            public RepositoryPullsViewModel Call()
            {
                var viewResult = (ViewResult)controller.Pulls(context);
                return (RepositoryPullsViewModel)viewResult.Model;
            }

            [Test]
            public void ReturnsDefaultView()
            {
                var actionResult = controller.Pulls(context);

                ActionResultAssert.IsViewOf<RepositoryPullsViewModel>(actionResult);
            }

            [Test]
            public void SetsHeader()
            {
                context.Header = new RepositoryHeaderViewModel();

                var viewModel = Call();

                Assert.AreEqual(context.Header, viewModel.Header);
            }

            [Test]
            public void SetsPullList()
            {
                var viewModels = new List<RepositoryPullDetailViewModel> { new RepositoryPullDetailViewModel() };
                pageService.GetPulls(Arg.Any<GetPullViewModelsRequest>()).Returns(viewModels);
                jsonSerializer.Serialize(viewModels).Returns("serialized");

                var viewModel = Call();

                pageService.Received().GetPulls(Args.EquivalentTo<GetPullViewModelsRequest>(context));
                Assert.AreEqual(viewModels, viewModel.List);
                Assert.AreEqual("serialized", viewModel.ListAsJson);
            }
        }

        public class Pull : RepositoryControllerTests
        {
            private string externalId;
            private ChangedIssuesResponse changedIssuesResponse;

            [SetUp]
            public void InitTest()
            {
                pageService.GetPull(Arg.Any<GetPullViewModelRequest>()).Returns(new RepositoryPullDetailViewModel());
                context.Repository = new Repository().New();
                context.Branch = new Branch().New();
                context.Header = new RepositoryHeaderViewModel();
                context.UrlHelper = controller.Url;
                repositoryService.GetPull(Arg.Any<GetPullRequest>()).Returns(new PullRequest());
                changedIssuesResponse = new ChangedIssuesResponse();
                pageService.GetPullRequestIssues(Arg.Any<GetPullRequestIssuesRequest>())
                    .Returns(changedIssuesResponse);
                pageService.GetBaseRepositoryIssues(Arg.Any<GetBaseRepositoryIssuesRequest>())
                    .Returns(new RepositoryIssuesViewModel
                             {
                                 ClientData = new RepositoryIssuesClientData
                                              {
                                                  NewProviderCommentLink = new PageLink(),
                                                  NewProviderIssueLink = new PageLink(),
                                                  ViewMoreLink = new PageLink()
                                              }
                             });
            }

            public RepositoryPullViewModel Call()
            {
                var viewResult = (ViewResult)controller.Pull(context, externalId);
                return (RepositoryPullViewModel)viewResult.Model;
            }

            [Test]
            public void ReturnsDefaultViewIfFound()
            {
                var actionResult = controller.Pull(context, externalId);

                ActionResultAssert.IsViewOf<RepositoryPullViewModel>(actionResult);
            }

            [Test]
            public void Returns404IfNotFound()
            {
                repositoryService.GetPull(Arg.Any<GetPullRequest>()).Returns(new NullPull());

                var actionResult = controller.Pull(context, externalId);

                ActionResultAssert.IsNotFound(actionResult);
            }

            [Test]
            public void SetsHeader()
            {
                context.Header = new RepositoryHeaderViewModel();

                var viewModel = Call();

                Assert.AreEqual(context.Header, viewModel.Header);
            }

            [Test]
            public void SetsPullViewModel()
            {
                externalId = "1";
                var detailViewModel = new RepositoryPullDetailViewModel();
                pageService.GetPull(Arg.Any<GetPullViewModelRequest>()).Returns(detailViewModel);
                var pullRequest = new PullRequest();
                repositoryService.GetPull(Arg.Any<GetPullRequest>()).Returns(pullRequest);

                Call();

                pageService.Received().GetPull(Args.EquivalentTo<GetPullViewModelRequest>(context, options => options
                    .Excluding(request => request.ExternalId)
                    .Excluding(request => request.PullRequest)));
                pageService.Received().GetPull(Arg
                    .Is<GetPullViewModelRequest>(request => request.ExternalId == "1" &&
                                                            request.PullRequest == pullRequest));
            }

            [Test]
            public void SetsPullsUrlAsActiveUrlForHeader()
            {
                context.Header.PullRequestsUrl = "pulls-url";

                Call();

                Assert.AreEqual(context.Header.PullRequestsUrl, context.Header.ActiveUrl);
            }
        }

        public class Settings : RepositoryControllerTests
        {
            [SetUp]
            public void InitTest()
            {
                context.Repository = new Repository().New();
                context.Branch = new Branch().New();
            }

            public RepositorySettingsViewModel Call()
            {
                var actionResult = (ViewResult)controller.Settings(context);
                return (RepositorySettingsViewModel)actionResult.Model;
            }

            [Test]
            public void ReturnsDefaultView()
            {
                var actionResult = controller.Settings(context);

                ActionResultAssert.IsViewOf<RepositorySettingsViewModel>(actionResult);
            }

            [Test]
            public void SetsHeader()
            {
                context.Header = new RepositoryHeaderViewModel();

                var viewModel = Call();

                Assert.AreEqual(context.Header, viewModel.Header);
            }

            [Test]
            public void SetsFields()
            {
                var model = new RepositorySettingsFields();
                pageService.GetSettingsFields(Arg.Any<GetSettingsRequest>()).Returns(model);
                jsonSerializer.Serialize(model).Returns("serialized");

                var viewModel = Call();

                pageService.Received().GetSettingsFields(Args.EquivalentTo<GetSettingsRequest>(context));
                Assert.AreEqual(model, viewModel.Fields);
                Assert.AreEqual("serialized", viewModel.FieldsAsJson);
            }
        }

        public class IgnoresFiles : RepositoryControllerTests
        {
            [SetUp]
            public void InitTest()
            {
                context.Repository = new Repository().New();
                context.Branch = new Branch().New();
                context.Header = new RepositoryHeaderViewModel
                                 {
                                     ActiveUrl = "active-url",
                                     SettingsUrl = "settings-url"
                                 };
            }

            public RepositoryIgnoreViewModel Call()
            {
                var actionResult = (ViewResult)controller.Ignore(context);
                return (RepositoryIgnoreViewModel)actionResult.Model;
            }

            [Test]
            public void ReturnsDefaultView()
            {
                var actionResult = controller.Ignore(context);

                ActionResultAssert.IsViewOf<RepositoryIgnoreViewModel>(actionResult);
            }

            [Test]
            public void SetsHeader()
            {
                context.Header = new RepositoryHeaderViewModel();

                var viewModel = Call();

                Assert.AreEqual(context.Header, viewModel.Header);
            }

            [Test]
            public void SetsFields()
            {
                var model = new RepositoryIgnoreFields();
                pageService.GetIgnoreFields(Arg.Any<GetSettingsRequest>()).Returns(model);
                jsonSerializer.Serialize(model).Returns("serialized");

                var viewModel = Call();

                pageService.Received().GetIgnoreFields(Args.EquivalentTo<GetSettingsRequest>(context));
                Assert.AreEqual(model, viewModel.Fields);
                Assert.AreEqual("serialized", viewModel.FieldsAsJson);
            }

            [Test]
            public void SetsActiveUrlAsSettings()
            {
                var viewModel = Call();

                Assert.AreEqual(context.Header.SettingsUrl, viewModel.Header.ActiveUrl);
            }
        }

        public class Integrations : RepositoryControllerTests
        {
            [SetUp]
            public void InitTest()
            {
                context.Repository = new Repository().New();
                context.Branch = new Branch().New();
                context.Header = new RepositoryHeaderViewModel
                                 {
                                     ActiveUrl = "active-url",
                                     SettingsUrl = "settings-url"
                                 };
            }

            public RepositoryIntegrationsViewModel Call()
            {
                var actionResult = (ViewResult)controller.Integrations(context);
                return (RepositoryIntegrationsViewModel)actionResult.Model;
            }

            [Test]
            public void ReturnsDefaultView()
            {
                var actionResult = controller.Integrations(context);

                ActionResultAssert.IsViewOf<RepositoryIntegrationsViewModel>(actionResult);
            }

            [Test]
            public void SetsHeader()
            {
                context.Header = new RepositoryHeaderViewModel();

                var viewModel = Call();

                Assert.AreEqual(context.Header, viewModel.Header);
            }

            [Test]
            public void SetsFields()
            {
                var model = new RepositoryIntegrationsFields();
                pageService.GetIntegrationsFields(Arg.Any<GetSettingsRequest>()).Returns(model);
                jsonSerializer.Serialize(model).Returns("serialized");

                var viewModel = Call();

                pageService.Received().GetIntegrationsFields(Args.EquivalentTo<GetSettingsRequest>(context));
                Assert.AreEqual(model, viewModel.Fields);
                Assert.AreEqual("serialized", viewModel.FieldsAsJson);
            }

            [Test]
            public void SetsActiveUrlAsSettings()
            {
                var viewModel = Call();

                Assert.AreEqual(context.Header.SettingsUrl, viewModel.Header.ActiveUrl);
            }
        }

        public class Rename : RepositoryControllerTests
        {
            private RepositoryRenameViewModel viewModel;

            [SetUp]
            public void InitTest()
            {
                context.UrlKey = Builder.Create<RepositoryUrlKey>();
                context.Repository = new Repository().New();
                viewModel = new RepositoryRenameViewModel();
            }

            public ActionResult Call()
            {
                return controller.Rename(context, viewModel);
            }

            public class FoundExistingRepository : Rename
            {
                [Test]
                public void ReturnsModelStateResult()
                {
                    repositoryService.GetByUrlKey(context.UrlKey).Returns(new Repository().New());
                    var jsonResult = new JsonResult();
                    modelStateErrorService.Get(controller.ModelState).Returns(jsonResult);

                    var actionResult = Call();

                    Assert.AreEqual(1, controller.ModelState.Count);
                    Assert.AreEqual(1, controller.ModelState.Single().Value.Errors.Count);
                    Assert.AreEqual("Name already exists on this account",
                        controller.ModelState.Single().Value.Errors[0].ErrorMessage);
                    Assert.AreEqual(jsonResult, actionResult);
                }

                [Test]
                public void DoesNotSaveRepository()
                {
                    repositoryService.GetByUrlKey(context.UrlKey).Returns(new Repository().New());

                    Call();

                    entityStorage.DidNotReceive().Set(Arg.Any<Repository>());
                }

                [Test]
                public void SelectRepositoryByNewName()
                {
                    repositoryService.GetByUrlKey(context.UrlKey).Returns(new Repository().New());
                    viewModel.Name = "new-name";
                    pageService.GetNormalizedName(viewModel.Name).Returns("normalized-name");

                    Call();

                    repositoryService.Received()
                        .GetByUrlKey(Arg.Is<RepositoryUrlKey>(key => key.Name == "normalized-name"));
                }
            }

            public class ValidNewRepositoryName : Rename
            {
                [Test]
                public void HasNoModelErrors()
                {
                    Call();

                    Assert.AreEqual(0, controller.ModelState.Count);
                }

                [Test]
                public void ChangesNameForRepositoryAndUrlKey()
                {
                    viewModel.Name = "new";
                    pageService.GetNormalizedName(viewModel.Name).Returns("normalized-name");

                    Call();

                    Assert.AreEqual("normalized-name", context.Repository.Name);
                    Assert.AreEqual("normalized-name", context.UrlKey.Name);
                }

                [Test]
                public void SavesUpdatedRepository()
                {
                    viewModel.Name = "new";
                    pageService.GetNormalizedName(viewModel.Name).Returns("normalized-name");

                    Call();

                    entityStorage.Received().Set(Arg.Is<Repository>(repository => repository.Name == "normalized-name"));
                }

                [Test]
                public void ReturnsRedirectToSettingsPageWithRenamedPath()
                {
                    viewModel.Name = "new";
                    pageService.GetNormalizedName(Arg.Any<string>()).Returns("new");
                    context.UrlKey.Name = "new";
                    context.UrlKey.BranchName = null;
                    controller.Url.ReturnsForAction(MVC.Repository.Settings(null), "new-settings-url",
                        RepositoryPage.AsRouteValues(context.UrlKey));

                    var actionResult = Call();

                    ActionResultAssert.IsJavaScriptRedirectTo("new-settings-url", actionResult);
                }

                [Test]
                public void SetsUrlBranchNameAsNull()
                {
                    Call();

                    Assert.AreEqual(null, context.UrlKey.BranchName);
                }
            }
        }

        public class SetDefaultBranch : RepositoryControllerTests
        {
            public string NewName { get; set; }

            [SetUp]
            public void InitTest()
            {
                branchService.Get(Arg.Any<GetBranchRequest>()).Returns(new NullBranch());
                NewName = "new";
            }

            public ActionResult Call()
            {
                return controller.SetDefaultBranch(context, NewName);
            }

            public class AnyBranch : SetDefaultBranch
            {
                [Test]
                public void CallsBranchService()
                {
                    Call();

                    branchService.Received()
                        .Get(Arg.Is<GetBranchRequest>(
                            request => request.Repository == context.Repository &&
                                       request.BranchName == NewName));
                }
            }

            public class NotFoundBranch : SetDefaultBranch
            {
                [Test]
                public void ReturnsNotFoundResult()
                {
                    branchService.Get(Arg.Any<GetBranchRequest>()).Returns(new NullBranch());
                    context.Repository = new Repository().New();

                    var actionResult = Call();

                    ActionResultAssert.IsNotFound(actionResult);
                }
            }

            public class FoundBranch : SetDefaultBranch
            {
                private Branch branch;

                [SetUp]
                public void InitSubTest()
                {
                    context.UrlKey = Builder.Create<RepositoryUrlKey>();
                    context.Repository = new Repository().New();
                    repositoryService.GetDefaultBranch(context.Repository).Returns(new NullBranch());
                    branch = new Branch { Name = "new" };
                    branchService.Get(Arg.Any<GetBranchRequest>()).Returns(branch);
                }

                [Test]
                public void ReturnsRedirectToSettingsPageWithRenamedPath()
                {
                    context.UrlKey.Name = "new";
                    context.UrlKey.BranchName = null;
                    controller.Url.ReturnsForAction(MVC.Repository.Settings(null), "new-settings-url",
                        RepositoryPage.AsRouteValues(context.UrlKey));

                    var actionResult = Call();

                    ActionResultAssert.IsJavaScriptRedirectTo("new-settings-url", actionResult);
                }

                [Test]
                public void ChangesDefaultBranchToNewOne()
                {
                    NewName = "new";
                    var currentDefaultBranch = new Branch { Name = "old", IsDefault = true };
                    context.Repository.Branches.Add(currentDefaultBranch);
                    repositoryService.GetDefaultBranch(context.Repository).Returns(currentDefaultBranch);

                    Call();

                    entityStorage.Received().Set(context.Repository);
                    Assert.AreEqual(true, branch.IsDefault);
                    Assert.AreEqual(false, currentDefaultBranch.IsDefault);
                }

                [Test]
                public void StartsAnalysisForNewDefaultBranch()
                {
                    branch.IsAnalyzed = false;
                    var currentDefaultBranch = new Branch { Name = "old" };
                    context.Repository.Branches.Add(currentDefaultBranch);
                    repositoryService.GetDefaultBranch(context.Repository).Returns(currentDefaultBranch);

                    Call();

                    branchService.Received()
                        .StartAnalysis(Arg.Is<StartAnalysisRequest>(
                            request => request.Repository == context.Repository &&
                                       request.Branch == branch));
                    Received.InOrder(() =>
                                     {
                                         entityStorage.Set(Arg.Any<Repository>());
                                         branchService.StartAnalysis(Arg.Any<StartAnalysisRequest>());
                                     });
                }
            }
        }

        public class Delete : RepositoryControllerTests
        {
            [SetUp]
            public void InitTest()
            {
                context.Repository = new Repository { UserKeys = new HashSet<string> { "1" } };
            }

            private ActionResult Call()
            {
                return controller.Delete(context, aspNetIdentity);
            }

            [Test]
            public void RedirectsToRepositoriesPage()
            {
                var actionResult = Call();

                ActionResultAssert.IsJavaScriptRedirectTo(MVC.Dashboard.Index(), actionResult);
            }

            [Test]
            public void CallServiceForRemoval()
            {
                aspNetIdentity.UserKey.Returns("uid");

                Call();

                repositoryService.Received()
                    .Delete(Args.EquivalentTo<DeleteRepositoryRequest>(context, options => options
                        .Excluding(request => request.UserKey)));
                repositoryService.Received()
                    .Delete(Arg.Is<DeleteRepositoryRequest>(request => request.UserKey == "uid"));
            }
        }

        public class SetExcludePatterns : RepositoryControllerTests
        {
            [SetUp]
            public void InitTest()
            {
                context.Repository = new Repository().New();
            }

            [Test]
            public void CallsSetIfPatternsAreChanged()
            {
                context.Repository.ExcludePatterns = new HashSet<string> { "p1" };
                var patterns = new HashSet<string> { "p1", "p2" };

                controller.SetExcludePatterns(context, patterns);

                repositoryService.Received()
                    .IgnorePatterns(Args.EquivalentTo<IgnorePatternsRequest>(context,
                        options => options.Excluding(request => request.List)));
            }

            [Test]
            public void DoesNotCallSetIfPatternsAreNotChanged()
            {
                context.Repository.ExcludePatterns = new HashSet<string> { "p1", "p2" };
                var patterns = new HashSet<string> { "p1", "p2" };

                controller.SetExcludePatterns(context, patterns);

                repositoryService.DidNotReceive().IgnorePatterns(Arg.Any<IgnorePatternsRequest>());
            }

            [Test]
            public void ReturnsEmptyResultForNullPaterns()
            {
                var actionResult = controller.SetExcludePatterns(context, null);

                Assert.IsInstanceOf<EmptyResult>(actionResult);
            }

            [Test]
            public void ReturnsEmptyResultForEmptyPatterns()
            {
                var actionResult = controller.SetExcludePatterns(context, new HashSet<string>());

                Assert.IsInstanceOf<EmptyResult>(actionResult);
            }

            [Test]
            public void ReturnsResponseWithMessageForChangedPatterns()
            {
                context.Repository.ExcludePatterns = new HashSet<string> { "p1" };
                var patterns = new HashSet<string> { "p1", "p2" };

                var actionResult = controller.SetExcludePatterns(context, patterns);

                Assert.IsInstanceOf<JsonResult>(actionResult);
                Assert.IsInstanceOf<SetExcludePatternsResponse>(((JsonResult)actionResult).Data);
            }
        }

        public class RefreshProvider : RepositoryControllerTests
        {
            [SetUp]
            public void InitTest()
            {
                context.Repository = new Repository().New();
            }

            private ActionResult Call()
            {
                return controller.RefreshProvider(context, aspNetIdentity);
            }

            [Test]
            public void ReturnsEmptyResultIfCannotRefresh()
            {
                repositoryService.CanRefreshProvider(context.Repository).Returns(false);

                var actionResult = Call();

                Assert.IsInstanceOf<EmptyResult>(actionResult);
            }

            [Test]
            public void CallsRefreshIfCanRefresh()
            {
                repositoryService.CanRefreshProvider(context.Repository).Returns(true);

                Call();

                pageService.Received().RefreshProvider(Arg.Is<RefreshProviderRequest>(
                    request => request.Repository == context.Repository &&
                               request.UrlHelper == controller.Url &&
                               request.UserKey == aspNetIdentity.UserKey));
            }

            [Test]
            public void ReturnsRefreshProviderResponseIfCanRefresh()
            {
                repositoryService.CanRefreshProvider(context.Repository).Returns(true);

                var actionResult = Call();

                Assert.IsInstanceOf<JsonResult>(actionResult);
                Assert.IsInstanceOf<RefreshProviderResponse>(((JsonResult)actionResult).Data);
            }
        }

        public class IgnoreCodeFile : RepositoryControllerTests
        {
            private string path = "path";

            [SetUp]
            public void InitTest()
            {
                context.Branch = new Branch().New();
                context.Repository = new Repository().New();
            }

            private ActionResult Call()
            {
                return controller.IgnoreCodeFile(context, path);
            }

            public class NotFoundFile : IgnoreCodeFile
            {
                [Test]
                public void ReturnsNotFoundResponse()
                {
                    branchService.GetCodeFile(Arg.Any<GetCodeFileRequest>())
                        .Returns((Core.Features.CodeFiles.CodeFile)null);

                    var actionResult = Call();

                    ActionResultAssert.IsNotFound(actionResult);
                }
            }

            public class FoundFile : IgnoreCodeFile
            {
                private Core.Features.CodeFiles.CodeFile codeFile;

                [SetUp]
                public void InitBranch()
                {
                    codeFile = new Core.Features.CodeFiles.CodeFile { Path = path };
                    branchService.GetCodeFile(Arg.Is<GetCodeFileRequest>(request => request.Branch == context.Branch &&
                                                                                    request.Path == path))
                        .Returns(codeFile);
                }

                [Test]
                public void ReturnsRedirectToFilesPage()
                {
                    var actionResult = Call();

                    ActionResultAssert.IsRedirectTo(MVC.Repository.Files(context), actionResult);
                }

                [Test]
                public void AddsFilePathToRepositoryExcludePatterns()
                {
                    Call();

                    Assert.Contains(path, context.Repository.ExcludePatterns.ToList());
                    repositoryService.Received()
                        .IgnorePatterns(Args.EquivalentTo<IgnorePatternsRequest>(context,
                            options => options.Excluding(request => request.List)));
                }

                [Test]
                public void DoesNotAddFileToExcludePatternIsAlreadyAdded()
                {
                    context.Repository.ExcludePatterns.Add(path);

                    Call();

                    Assert.AreEqual(1, context.Repository.ExcludePatterns.Count);
                    repositoryService.DidNotReceive().IgnorePatterns(Arg.Any<IgnorePatternsRequest>());
                }

                [Test]
                public void IsSetAsHidden()
                {
                    Call();

                    branchService.Received().HideCodeFile(codeFile);
                }
            }
        }

        public class NewProviderIssue : RepositoryControllerTests
        {
            private NewProviderIssueViewModel viewModel;

            [SetUp]
            public void InitTest()
            {
                viewModel = new NewProviderIssueViewModel();
            }

            [Test]
            public void ReturnsResultFromPageService()
            {
                var response = new CreateNewProviderIssueResponse();
                pageService.CreateNewProviderIssue(Arg.Any<CreateNewProviderIssueRequest>()).Returns(response);
                var issue = new Issue();
                viewModel.Keys = new HashSet<string> { "k1" };
                issuesService.GetByKeys(viewModel.Keys).Returns(new List<Issue> { issue });

                var actionResult = controller.NewProviderIssue(context, viewModel, aspNetIdentity);

                pageService.Received()
                    .CreateNewProviderIssue(
                        Arg.Is<CreateNewProviderIssueRequest>(request =>
                            request.Repository == context.Repository &&
                            request.Issues[0] == issue &&
                            request.ViewModel == viewModel &&
                            request.UserKey == aspNetIdentity.UserKey));

                ActionResultAssert.IsJsonResult(actionResult, response);
            }

            [Test]
            public void ReturnsNotFoundIfIssueIsNull()
            {
                viewModel.Keys = new HashSet<string> { "unknown" };

                var actionResult = controller.NewProviderIssue(context, viewModel, aspNetIdentity);

                ActionResultAssert.IsNotFound(actionResult);
            }
        }

        public class NewProviderComment : RepositoryControllerTests
        {
            private NewProviderCommentViewModel viewModel;

            [SetUp]
            public void InitTest()
            {
                viewModel = new NewProviderCommentViewModel();
            }

            [Test]
            public void ReturnsResultFromPageService()
            {
                var response = new CreateNewProviderCommentResponse();
                pageService.CreateNewProviderComment(Arg.Any<CreateNewProviderCommentRequest>()).Returns(response);
                var issue = new Issue();
                viewModel.Keys = new HashSet<string> { "k1" };
                issuesService.GetByKeys(viewModel.Keys).Returns(new List<Issue> { issue });

                var actionResult = controller.NewProviderComment(context, viewModel, aspNetIdentity);

                pageService.Received()
                    .CreateNewProviderComment(
                        Arg.Is<CreateNewProviderCommentRequest>(request =>
                            request.Repository == context.Repository &&
                            request.Issues[0] == issue &&
                            request.ViewModel == viewModel &&
                            request.UserKey == aspNetIdentity.UserKey));

                ActionResultAssert.IsJsonResult(actionResult, response);
            }

            [Test]
            public void ReturnsNotFoundIfIssueIsNull()
            {
                viewModel.Keys = new HashSet<string> { "unknown" };

                var actionResult = controller.NewProviderComment(context, viewModel, aspNetIdentity);

                ActionResultAssert.IsNotFound(actionResult);
            }
        }

        public class ToggleBranchAnalysis : RepositoryControllerTests
        {
            private ToggleBranchAnalysisViewModel viewModel;

            [SetUp]
            public void InitTest()
            {
                viewModel = Builder.Create<ToggleBranchAnalysisViewModel>();
                context.Repository = new Repository().New();
            }

            private ActionResult Call()
            {
                return controller.ToggleBranchAnalysis(context, viewModel);
            }

            [Test]
            public void ReturnsEmptyResultByDefault()
            {
                var actionResult = Call();

                Assert.IsInstanceOf<EmptyResult>(actionResult);
            }

            [Test]
            public void ReturnsNotFoundResultIfBranchIsNull()
            {
                branchService.Get(Arg.Any<GetBranchRequest>()).Returns(new NullBranch());

                var actionResult = Call();

                branchService.Received().Get(Arg.Is<GetBranchRequest>(
                    request => request.Repository == context.Repository && request.BranchName == viewModel.Name));
                ActionResultAssert.IsNotFound(actionResult);
            }

            [Test]
            public void StartsAnalysisForStartAction()
            {
                viewModel.Action = BranchAnalysisAction.Start;
                var branch = new Branch().New();
                branchService.Get(Arg.Any<GetBranchRequest>()).Returns(branch);

                Call();

                branchService.Received()
                    .StartAnalysis(Arg.Is<StartAnalysisRequest>(
                        request => request.Repository == context.Repository && request.Branch == branch));
            }

            [Test]
            public void StopsAnalysisForStopAction()
            {
                viewModel.Action = BranchAnalysisAction.Stop;
                var branch = new Branch().New();
                branchService.Get(Arg.Any<GetBranchRequest>()).Returns(branch);

                Call();

                branchService.Received()
                    .StopAnalysis(Arg.Is<StopAnalysisRequest>(
                        request => request.Repository == context.Repository && request.Branch == branch));
            }

            [Test, ExpectedException(typeof(NotSupportedException))]
            public void ThrowsForUnknownAction()
            {
                viewModel.Action = 0;

                Call();
            }
        }

        public class ViewMore : RepositoryControllerTests
        {
            private ViewMoreViewModel viewModel;

            [SetUp]
            public void InitTest()
            {
                viewModel = Builder.Create<ViewMoreViewModel>();
            }

            public JsonResult Call()
            {
                return (JsonResult)controller.ViewMore(context, viewModel);
            }

            public class NotFoundCodeFile : ViewMore
            {
                [SetUp]
                public void InitSubTest()
                {
                    branchService.GetCodeFile(Arg.Any<GetCodeFileRequest>())
                        .Returns((Core.Features.CodeFiles.CodeFile)null);
                }

                [Test]
                public void Returns404()
                {
                    var actionResult = controller.ViewMore(context, viewModel);

                    ActionResultAssert.IsNotFound(actionResult);
                    branchService.Received()
                        .GetCodeFile(Arg.Is<GetCodeFileRequest>(request => request.Branch == context.Branch &&
                                                                           request.Path == viewModel.FilePath));
                }

                [Test]
                public void DoesNotRetrieveFileContent()
                {
                    controller.ViewMore(context, viewModel);

                    pageService.DidNotReceive().GetCodeFileContent(Arg.Any<GetCodeFileContentRequest>());
                }
            }

            public class FoundCodeFile : ViewMore
            {
                private Core.Features.CodeFiles.CodeFile codeFile;

                [SetUp]
                public void InitSubTest()
                {
                    codeFile = new Core.Features.CodeFiles.CodeFile();
                    branchService.GetCodeFile(Arg.Any<GetCodeFileRequest>()).Returns(codeFile);
                }

                [Test]
                public void ReturnsContentResponseFromPageService()
                {
                    var response = new ReadCodeLinesResponse();
                    pageService.GetCodeFileContent(Arg.Any<GetCodeFileContentRequest>()).Returns(response);

                    var jsonResult = Call();

                    Assert.AreEqual(response, jsonResult.Data);
                    pageService.Received().GetCodeFileContent(Args
                        .EquivalentTo<GetCodeFileContentRequest>(context, options => options
                            .Excluding(request => request.StartLine)
                            .Excluding(request => request.EndLine)));
                    pageService.Received().GetCodeFileContent(Args
                        .EquivalentTo<GetCodeFileContentRequest>(viewModel, options => options
                            .Excluding(request => request.Branch)
                            .Excluding(request => request.UrlKey)));
                    pageService.Received()
                        .GetCodeFileContent(
                            Arg.Is<GetCodeFileContentRequest>(request => request.UrlKey.FilePath == viewModel.FilePath));
                }
            }
        }

        public class TrendsForIssues : RepositoryControllerTests
        {
            [SetUp]
            public void InitTest()
            {
                context.Repository = new Repository().New();
                context.Branch = new Branch().New();
                context.Header = new RepositoryHeaderViewModel();
                pageService.GetTrends(Arg.Any<GetTrendsViewModelsRequest>()).Returns(new RepositoryTrendsViewModel());
            }

            public RepositoryTrendsViewModel Call()
            {
                var actionResult = (ViewResult)controller.TrendsForIssues(context);
                return (RepositoryTrendsViewModel)actionResult.Model;
            }

            [Test]
            public void ReturnsDefaultView()
            {
                var actionResult = controller.TrendsForIssues(context);

                ActionResultAssert.IsViewOf<RepositoryTrendsViewModel>(actionResult);
            }

            [Test]
            public void SetsViewModel()
            {
                Call();

                pageService.Received()
                    .GetTrends(Args.EquivalentTo<GetTrendsViewModelsRequest>(context, options => options
                        .Excluding(request => request.TrendsType)));
            }
        }
    }
}
