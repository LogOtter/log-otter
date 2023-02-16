<script lang="ts">
import { defineComponent } from "vue";
import App from "./App.vue";

export default defineComponent({
  components: {
    App,
  },
});
</script>

<template>
  <Suspense>
    <app></app>
    <template #fallback>
      <div class="loading">Loading...</div>
    </template>
  </Suspense>
</template>

<style>
.loading {
  flex-grow: 1;
  align-self: center;
  text-align: center;
}
</style>
