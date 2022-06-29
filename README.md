| Pipeline Name | Pipeline Status |
| ------------- | --------------- |
| CEDAR.Core.Collector-GitHub.Validation | [![Build Status](https://dev.azure.com/mseng/Domino/_apis/build/status/CloudMine/Pipelines/GitHub/Collectors/CEDAR.Core.Collector-GitHub.Validation?repoName=microsoft%2FCEDAR.Core.Collector&branchName=main)](https://dev.azure.com/mseng/Domino/_build/latest?definitionId=10590&repoName=microsoft%2FCEDAR.Core.Collector&branchName=main) |
| CEDAR.Core.Collector-AzureDevOps.Validation | [![Build Status](https://dev.azure.com/mseng/Domino/_apis/build/status/CloudMine/Pipelines/Azure%20DevOps/Collectors/vKivanc/CEDAR.Core.Collector-AzureDevOps.Validation?repoName=microsoft%2FCEDAR.Core.Collector&branchName=main)](https://dev.azure.com/mseng/Domino/_build/latest?definitionId=10568&repoName=microsoft%2FCEDAR.Core.Collector&branchName=main) |

# Introduction

CEDAR.Core.Collector is the core library that is shared among all CEDAR collectors. It abstract commit REST-API-based data collection paradigm such as:
* Making HTTP requests and processing response
* Processing/handling JSON-based HTTP web responses
* Automatically batching web requests, where applicable, with the ability to halt batching after each request
* Automatically following/utilizing various continuation token implementations: part of response headers, part of response, oData, etc.
* Abstracting graph-based data collection

Currently, there are two products that built on CEDAR.Core.Collector:
1. [CEDAR.GitHub.Collector](https://github.com/microsoft/CEDAR.GitHub.Collector): Data collectors for GitHub.
2. [CEDAR.AzureDevOps.Collector](https://dev.azure.com/mseng/Domino/_git/CloudMine): Data collectors for AzureDevOps (this product is not open source and therefore only Microsoft employees can view this link).

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
