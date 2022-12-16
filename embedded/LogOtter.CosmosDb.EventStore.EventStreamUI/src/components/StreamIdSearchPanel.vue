<script lang="ts">
import { defineComponent } from "vue";

export default defineComponent({
  data() {
    return {
      streamId: "",
    };
  },
  emits: ["search"],
  props: {
    startingStreamId: String,
  },
  methods: {
    submit() {
      if (!this.streamId.trim()) {
        return;
      }

      this.$emit("search", this.streamId);
    },
  },
  mounted() {
    this.streamId = this.startingStreamId || "";
    var search = this.$refs.search as any;
    search.focus();
  },
});
</script>

<template>
  <div class="card mb-3">
    <div class="card-header">
      <label for="search">Stream ID</label>
    </div>
    <div class="card-body">
      <div class="mb-3">
        <form @submit.prevent="submit">
          <div class="input-group">
            <input
              type="text"
              ref="search"
              id="search"
              class="form-control"
              placeholder="Enter stream ID"
              v-model="streamId"
            />
            <button class="btn btn-outline-secondary" type="submit">
              <i class="bi-search"></i>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
