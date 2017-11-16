# Contributing to Windows 10 IoT Core Samples

Thank you for your interest in our documentation. We appreciate your feedback, edits, additions and help with improving our samples. This page covers the basic steps and guidelines for contributing.

## Sign a CLA

If you want to contribute more than a couple lines and you're not a Microsoft employee, you need to [sign a Microsoft Contribution Licensing Agreement (CLA)](https://cla.microsoft.com/). 

## Proposing a change

To suggest a change to the samples, follow these steps:

1. From the GitHub page you would like to change, click the pencil icon.
2. If you are already in the GitHub repo, you can just navigate to the source file that you would like to change.
3. If you don't already have a GitHub account, click **Sign Up** in the upper right and create a new account.
4. Modify the file and use the preview tab to ensure the changes look good.
5. When you're done, commit your changes and open a pull request.

After you create the pull request, a member of the Windows 10 IoT team will review. 

*For employees*:
When submitting a PR, please be sure to create a new branch. Then, merge changes to develop.

*For non-employees*:
You will need to fork and clone this repo to your local machine before submitting a PR. Please see guidelines below.

## Submitting a new sample

If you are looking to submit a new sample, please be sure to follow the format of the samples currently in this repository. A standard sample folder should contain the following:

* Folder(s) with respective language(s)
* Read.me linking to different language(s) sample(s) and additional resources

Each language folder should contain:

* Project files (if applicable)
* Image files (if applicable)
* README.md which details steps to take for sample (if applicable)

## Authoring your contribution

Once you have forked and cloned the repo to your local machine, you can begin authoring with the text editor of your choice.  We, of course, recommend [Visual Studio Code](https://code.visualstudio.com/), a free lightweight open source editor from Microsoft. 

## Submitting your contribution and filing a Pull Request (PR)

Once you are ready to add your changes to the remote repo so that they will be staged for publishing, enter the following steps in the command line:
- `git status`: This command will show you what files you have changed so that you can confirm that you intended to make those changes. 
- `git add -A`: This command tells git to add ALL of your changes. If you would prefer to only add the changes you have made to one particular file, instead enter the command: `git add <file.md>`, where "file.md" represents the name the file containing your changes.
- `git commit -m “Fixed a few typos”`: This command tells git to commit the changes that you added in the previous step, along with a short message describing the changes that you made.
- `git push origin <yourbranchname>`: This command pushes your changes to the remote repo that you forked on GitHub (the "origin") into the branch that you have specified. Because you have forked the repo to your own GitHub account, you are welcome to do your work in the **Develop** branch. 

When you are happy with your changes and ready to submit a PR:
- Go to your fork of the IoT repo: https://github.com/your-github-alias/windows-iotcore-docs.
- Click the "New pull request" button. (The "base fork:" will be listed as "MicrosoftDocs/windows-iotcore-docs", the "head fork:" should show your fork of the repo and the branch in which you made your changes.) You can review your changes here as well. 
- Click the green "Create pull request" button. You will then be asked to give your Pull Request a title and description, then click the "Create pull request" button once more.

Once your PR is submitted, a member of the Windows 10 IoT team will review. 

## Using issues to provide feedback on IoT documentation

To provide feedback rather than directly modifying actual documentation pages, [create an issue](https://github.com/Microsoft/Windows-iotcore-samples/issues).

Be sure to include the topic title and the URL for the page.

## Additional resources
- [Getting started with writing and formatting on GitHub](https://help.github.com/articles/getting-started-with-writing-and-formatting-on-github/)

## Additional resources for Microsoft employees
- [Connect your GitHub account and MS alias](https://review.docs.microsoft.com/en-us/windows-authoring-guide/github-account#2-connect-your-github-account-and-ms-alias-on-the-microsoft-open-source-portal)
- [Resources for writing Markdown](https://review.docs.microsoft.com/en-us/windows-authoring-guide/writing-guidance/writing-markdown)

