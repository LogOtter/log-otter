<script lang="ts">
import { defineComponent, onErrorCaptured, ref } from "vue";
import App from "./App.vue";

export default defineComponent({
  setup() {
    const error = ref();

    onErrorCaptured((e) => {
      error.value = e;
      return true;
    });

    return { error };
  },
  components: {
    App,
  },
});
</script>

<template>
  <div class="loading" v-if="error"><strong>Error</strong> - Could not connect to service</div>
  <Suspense>
    <app></app>
    <template #fallback>
      <div class="loading" v-if="!error">Loading...</div>
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
