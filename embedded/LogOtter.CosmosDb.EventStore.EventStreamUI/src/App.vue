<script lang="ts">
import { defineComponent } from "vue";
import SideBar from "./components/SideBar.vue";
import RootPage from "./pages/RootPage.vue";
import { EventStreamsServiceFactory } from "./services/EventStreamsServiceFactory";

export default defineComponent({
  async setup() {
    const eventStreamsService = await new EventStreamsServiceFactory().create();
    return { eventStreamsService };
  },
  components: {
    SideBar,
    RootPage,
  },
  provide() {
    return { eventStreamsService: this.eventStreamsService };
  },
});
</script>

<template>
  <side-bar></side-bar>
  <root-page></root-page>
</template>
