module.exports = ({github, context}) => {
  const {data: comments} = await github.rest.issues.listComments({
    owner: context.repo.owner,
    repo: context.repo.repo,
    issue_number: context.issue.number,
  });

  const botComment = comments.find(comment => {
    return comment.user.type === 'Bot' && comment.body.includes('Terraform Format and Style')
  });

  const output = `#### Terraform Format and Style 🖌 \`${{steps.fmt.outcome}}\`
#### Terraform Initialization ⚙️ \`${{steps.init.outcome}}\`
#### Terraform Validation 🤖 \`${{steps.validate.outcome}}\`
<details><summary>Validation Output</summary>

\`\`\`\n
${{steps.validate.outputs.stdout}}
\`\`\`

</details>

#### Terraform Plan 📖 \`${{steps.plan.outcome}}\`

<details><summary>Show Plan</summary>

\`\`\`\n
${{steps.plan.outputs.stdout}}
\`\`\`

</details>

*Pusher: @${{github.actor}}*, Last Updated: ${{new Date().toISOString()}}`;

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
