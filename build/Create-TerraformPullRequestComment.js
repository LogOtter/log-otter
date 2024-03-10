module.exports = async ({github, context, outcome}) => {
  const {data: comments} = await github.rest.issues.listComments({
    owner: context.repo.owner,
    repo: context.repo.repo,
    issue_number: context.issue.number,
  });

  const botComment = comments.find(comment => {
    return comment.user.type === 'Bot' && comment.body.includes('Terraform Format and Style')
  });

  const output = `#### Terraform Format and Style 🖌 \`${outcome.fmt ? '✅' : '❌'}\`
#### Terraform Initialization ⚙️ \`${outcome.init ? '✅' : '❌'}\`
#### Terraform Validation 🤖 \`${outcome.validate.result ? '✅' : '❌'}\`
<details><summary>Validation Output</summary>

\`\`\`\n
${outcome.validate.stdout}
\`\`\`

</details>

#### Terraform Plan 📖 \`${outcome.plan.result ? '✅' : '❌'}\`

<details><summary>Show Plan</summary>

\`\`\`\n
${outcome.plan.stdout}
\`\`\`

</details>

*Pusher: @${github.actor}*, Last Updated: ${new Date().toISOString()}`;

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
