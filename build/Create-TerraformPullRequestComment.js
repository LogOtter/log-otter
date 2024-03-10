module.exports = async ({github, context, outcome}) => {
  const {data: comments} = await github.rest.issues.listComments({
    owner: context.repo.owner,
    repo: context.repo.repo,
    issue_number: context.issue.number,
  });

  const botComment = comments.find(comment => {
    return comment.user.type === 'Bot' && comment.body.includes('Terraform Format and Style')
  });

  const output = `#### ${outcome.fmt ? '✅' : '❌'} Terraform Format and Style 🖌
#### ${outcome.init ? '✅' : '❌'} Terraform Initialization ⚙️
#### ${outcome.validate ? '✅' : '❌'} Terraform Validation 🤖
<details><summary>Validation Output</summary>

\`\`\`\n
${process.env.VALIDATE_OUTPUT}
\`\`\`

</details>

#### ${outcome.plan ? '✅' : '❌'} Terraform Plan 📖

<details><summary>Show Plan</summary>

\`\`\`\n
${process.env.PLAN_OUTPUT}
\`\`\`

</details>

*Pusher:* @${context.actor}
*Last Updated:* \`${new Date().toISOString()}\``;

  if (botComment) {
    github.rest.issues.updateComment({
      owner: context.repo.owner,
      repo: context.repo.repo,
      comment_id: botComment.id,
      body: output
    });
  } else {
    github.rest.issues.createComment({
      issue_number: context.issue.number,
      owner: context.repo.owner,
      repo: context.repo.repo,
      body: output
    });
  }
}
